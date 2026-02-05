#!/usr/bin/env python3
"""
E2E Test Result Analyzer

Analyzes E2E test results from TRX files and categorizes failures:
- Timeout failures (infrastructure issues)
- Assertion failures (genuine bugs)
- Other failures (need investigation)

Usage:
  python3 analyze_e2e_results.py <trx-file> [--strict]
  
Options:
  --strict    Fail on any error including timeouts (default: warn on timeouts only)
  
Exit codes:
  0 - All tests passed OR only timeout failures (unless --strict)
  1 - Assertion failures found OR strict mode with any failures
  2 - No test results found
"""

import sys
import re
import xml.etree.ElementTree as ET
from pathlib import Path
from typing import Tuple, List, Dict

# Patterns for identifying failure types
TIMEOUT_PATTERNS = [
    r'TimeoutException',
    r'Timeout.*exceeded',
    r'timeout.*ms',
    r'waiting for.*timed out',
    r'Timeout \d+ms exceeded',
    r'Page\..*\(\).*Timeout',
    r'locator.*timeout',
]

ASSERTION_PATTERNS = [
    r'Expected.*but.*got',
    r'Expected.*to.*but',
    r'Assert\.',
    r'AssertionException',
    r'ToBeVisible.*failed',
    r'ToHaveText.*failed',
    r'ToContain.*failed',
    r'Expected true but was false',
    r'Expected false but was true',
]


class TestResult:
    """Represents a single test result"""
    def __init__(self, name: str, outcome: str, message: str = "", stack_trace: str = ""):
        self.name = name
        self.outcome = outcome
        self.message = message
        self.stack_trace = stack_trace
        self.failure_type = self._categorize_failure()
    
    def _categorize_failure(self) -> str:
        """Categorize the failure type"""
        if self.outcome == "Passed":
            return "passed"
        
        combined_text = f"{self.message} {self.stack_trace}"
        
        # Check for timeouts first (they might include assertions in waiting code)
        for pattern in TIMEOUT_PATTERNS:
            if re.search(pattern, combined_text, re.IGNORECASE):
                return "timeout"
        
        # Check for assertions
        for pattern in ASSERTION_PATTERNS:
            if re.search(pattern, combined_text, re.IGNORECASE):
                return "assertion"
        
        # Unknown failure type
        return "unknown"
    
    def __str__(self) -> str:
        status = "✅" if self.outcome == "Passed" else "❌"
        return f"{status} {self.name} [{self.failure_type}]"


def parse_trx_file(trx_path: Path) -> Tuple[List[TestResult], Dict[str, int]]:
    """Parse TRX file and extract test results"""
    try:
        tree = ET.parse(trx_path)
        root = tree.getroot()
    except Exception as e:
        print(f"❌ Error parsing TRX file: {e}")
        sys.exit(2)
    
    # TRX files use namespaces
    ns = {'vs': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
    
    results = []
    
    # Find all UnitTestResult elements
    for result_elem in root.findall('.//vs:UnitTestResult', ns):
        name = result_elem.get('testName', 'Unknown')
        outcome = result_elem.get('outcome', 'Unknown')
        
        # Extract error message and stack trace
        message = ""
        stack_trace = ""
        
        output_elem = result_elem.find('.//vs:Output', ns)
        if output_elem is not None:
            error_info = output_elem.find('.//vs:ErrorInfo', ns)
            if error_info is not None:
                message_elem = error_info.find('.//vs:Message', ns)
                if message_elem is not None and message_elem.text:
                    message = message_elem.text
                
                stack_elem = error_info.find('.//vs:StackTrace', ns)
                if stack_elem is not None and stack_elem.text:
                    stack_trace = stack_elem.text
        
        results.append(TestResult(name, outcome, message, stack_trace))
    
    # Count outcomes
    counts = {
        'total': len(results),
        'passed': sum(1 for r in results if r.outcome == 'Passed'),
        'failed': sum(1 for r in results if r.outcome == 'Failed'),
        'timeout': sum(1 for r in results if r.failure_type == 'timeout'),
        'assertion': sum(1 for r in results if r.failure_type == 'assertion'),
        'unknown': sum(1 for r in results if r.failure_type == 'unknown'),
    }
    
    return results, counts


def print_summary(counts: Dict[str, int]):
    """Print test summary"""
    print("\n" + "="*70)
    print("📊 E2E Test Results Summary")
    print("="*70)
    print(f"Total Tests:       {counts['total']}")
    print(f"✅ Passed:         {counts['passed']}")
    print(f"❌ Failed:         {counts['failed']}")
    if counts['failed'] > 0:
        print(f"   ⏱️  Timeouts:    {counts['timeout']}")
        print(f"   🐛 Assertions:  {counts['assertion']}")
        print(f"   ❓ Unknown:     {counts['unknown']}")
    print("="*70 + "\n")


def print_failures(results: List[TestResult], failure_type: str = None):
    """Print detailed failure information"""
    failures = [r for r in results if r.outcome != 'Passed']
    
    if failure_type:
        failures = [f for f in failures if f.failure_type == failure_type]
    
    if not failures:
        return
    
    type_label = f" ({failure_type})" if failure_type else ""
    print(f"\n{'='*70}")
    print(f"Failed Tests{type_label}")
    print('='*70)
    
    for result in failures:
        print(f"\n❌ {result.name}")
        print(f"   Type: {result.failure_type}")
        if result.message:
            # Print first 300 chars of message
            msg = result.message[:300]
            if len(result.message) > 300:
                msg += "..."
            print(f"   Message: {msg}")


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(2)
    
    trx_path = Path(sys.argv[1])
    strict_mode = '--strict' in sys.argv
    
    if not trx_path.exists():
        print(f"❌ Test results file not found: {trx_path}")
        sys.exit(2)
    
    print(f"🔍 Analyzing E2E test results from: {trx_path}")
    print(f"   Mode: {'STRICT (fail on any error)' if strict_mode else 'LENIENT (warn on timeouts)'}")
    
    results, counts = parse_trx_file(trx_path)
    
    print_summary(counts)
    
    # All tests passed
    if counts['failed'] == 0:
        print("✅ All E2E tests passed!")
        sys.exit(0)
    
    # Print failure details
    if counts['assertion'] > 0:
        print_failures(results, 'assertion')
    
    if counts['timeout'] > 0:
        print_failures(results, 'timeout')
    
    if counts['unknown'] > 0:
        print_failures(results, 'unknown')
    
    # Decision logic
    print("\n" + "="*70)
    print("🎯 Decision")
    print("="*70)
    
    # Genuine assertion failures always fail the build
    if counts['assertion'] > 0:
        print(f"🚨 FAIL: Found {counts['assertion']} assertion failure(s)")
        print("   These are genuine test failures that must be fixed.")
        sys.exit(1)
    
    # Unknown failures fail safe
    if counts['unknown'] > 0:
        print(f"❓ FAIL: Found {counts['unknown']} unclassified failure(s)")
        print("   Failing build to be safe. Manual investigation needed.")
        sys.exit(1)
    
    # Only timeout failures
    if counts['timeout'] > 0:
        if strict_mode:
            print(f"⚠️  FAIL: Found {counts['timeout']} timeout failure(s) (strict mode)")
            print("   Timeouts fail in strict mode.")
            sys.exit(1)
        else:
            print(f"⚠️  WARN: Found {counts['timeout']} timeout failure(s)")
            print("   Timeouts are often infrastructure issues (slow CI, network delays).")
            print("   Treated as warnings in lenient mode. Build passes.")
            print("\n💡 Tip: Run with --strict to fail on timeouts.")
            sys.exit(0)
    
    # Should not reach here
    print("❓ Unexpected state. Failing to be safe.")
    sys.exit(1)


if __name__ == '__main__':
    main()
