#!/usr/bin/env python3
"""
Fix benchmark data on gh-pages by merging old chart set names into new ones.

Problem: The benchmark chart names were changed from:
- "Virtual DOM Benchmarks" -> "Rendering Engine Throughput"
- "Virtual DOM Allocations" -> "Rendering Engine Allocations"

This resulted in two separate chart sets - the old ones stopped receiving updates.

Solution: Merge the old entries into the new chart sets, sorted by date.
"""

import json
import sys
from pathlib import Path


def fix_benchmark_data(input_file: str, output_file: str) -> None:
    """Read the benchmark data, merge old names into new names, and write output."""
    
    # Read the data.js file
    content = Path(input_file).read_text()
    
    # Remove the JavaScript wrapper
    if content.startswith("window.BENCHMARK_DATA = "):
        json_str = content.replace("window.BENCHMARK_DATA = ", "", 1)
    else:
        json_str = content
    
    data = json.loads(json_str)
    
    entries = data.get("entries", {})
    
    # Mapping of old names to new names
    name_mapping = {
        "Virtual DOM Benchmarks": "Rendering Engine Throughput",
        "Virtual DOM Allocations": "Rendering Engine Allocations",
    }
    
    # Merge old entries into new entries
    for old_name, new_name in name_mapping.items():
        if old_name in entries:
            old_entries = entries[old_name]
            
            # Get or create the new entries list
            if new_name not in entries:
                entries[new_name] = []
            
            # Get existing commit IDs to avoid duplicates
            existing_ids = {e["commit"]["id"] for e in entries[new_name] if "commit" in e and "id" in e["commit"]}
            
            # Add old entries that aren't duplicates
            for entry in old_entries:
                commit_id = entry.get("commit", {}).get("id", "")
                if commit_id and commit_id not in existing_ids:
                    entries[new_name].append(entry)
                    existing_ids.add(commit_id)
            
            # Remove the old entry
            del entries[old_name]
            
            print(f"Merged {len(old_entries)} entries from '{old_name}' into '{new_name}'")
    
    # Sort each entry list by date
    for name in entries:
        entries[name].sort(key=lambda e: e.get("date", 0))
        print(f"'{name}': {len(entries[name])} total entries")
    
    data["entries"] = entries
    
    # Write the output with the JavaScript wrapper
    # Use ensure_ascii=False to preserve ± symbols without escaping
    output_content = "window.BENCHMARK_DATA = " + json.dumps(data, indent=2, ensure_ascii=False)
    Path(output_file).write_text(output_content, encoding='utf-8')
    
    print(f"\nFixed data written to: {output_file}")


def main():
    if len(sys.argv) < 3:
        print(f"Usage: {sys.argv[0]} <input_data.js> <output_data.js>")
        print(f"Example: {sys.argv[0]} /tmp/benchmark-data.js /tmp/fixed-data.js")
        sys.exit(1)
    
    input_file = sys.argv[1]
    output_file = sys.argv[2]
    
    fix_benchmark_data(input_file, output_file)


if __name__ == "__main__":
    main()
