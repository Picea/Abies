#!/usr/bin/env bash
# =============================================================================
# Local Benchmark Runner for Abies Framework
# =============================================================================
# This script provides a consistent way to run benchmarks locally before
# pushing to a PR. It implements the dual-layer benchmarking strategy:
#
# 1. MICRO-BENCHMARKS (BenchmarkDotNet) - Fast feedback
# 2. E2E BENCHMARKS (js-framework-benchmark) - Source of truth
#
# Usage:
#   ./scripts/run-benchmarks.sh [options]
#
# Options:
#   --micro          Run micro-benchmarks only (BenchmarkDotNet)
#   --e2e            Run E2E benchmarks only (js-framework-benchmark)
#   --all            Run both micro and E2E benchmarks
#   --quick          Run quick micro-benchmarks (fewer iterations)
#   --compare        Compare against baseline (requires previous run)
#   --update-baseline Update the E2E baseline with current results
#   --help           Show this help message
#
# Examples:
#   ./scripts/run-benchmarks.sh --micro          # Quick feedback during dev
#   ./scripts/run-benchmarks.sh --e2e            # Before merging perf PR
#   ./scripts/run-benchmarks.sh --all            # Full validation
# =============================================================================

set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
JS_BENCHMARK_DIR="${ROOT_DIR}/../js-framework-benchmark-fork"
RESULTS_DIR="${ROOT_DIR}/benchmark-results/local"
BASELINE_FILE="${ROOT_DIR}/benchmark-results/baseline.json"

# Default options
RUN_MICRO=false
RUN_E2E=false
QUICK_MODE=false
COMPARE_MODE=false
UPDATE_BASELINE=false

# =============================================================================
# Helper Functions
# =============================================================================

print_header() {
    echo ""
    echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
    echo -e "${BLUE}  $1${NC}"
    echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
    echo ""
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

print_info() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

show_help() {
    head -40 "$0" | grep -E "^#" | sed 's/^# //' | sed 's/^#//'
    exit 0
}

check_prerequisites() {
    print_header "Checking Prerequisites"
    
    # Check .NET SDK
    if ! command -v dotnet &> /dev/null; then
        print_error ".NET SDK not found. Please install .NET 10.0+"
        exit 1
    fi
    print_success ".NET SDK: $(dotnet --version)"
    
    # Check Python
    if ! command -v python3 &> /dev/null; then
        print_error "Python 3 not found. Please install Python 3.10+"
        exit 1
    fi
    print_success "Python: $(python3 --version)"
    
    # Check Node.js (for E2E)
    if $RUN_E2E; then
        if ! command -v node &> /dev/null; then
            print_error "Node.js not found. Please install Node.js 20+"
            exit 1
        fi
        print_success "Node.js: $(node --version)"
        
        # Check js-framework-benchmark directory
        if [[ ! -d "$JS_BENCHMARK_DIR" ]]; then
            print_warning "js-framework-benchmark not found at: $JS_BENCHMARK_DIR"
            print_info "Clone it with: git clone https://github.com/nicknash/js-framework-benchmark.git ../js-framework-benchmark-fork"
            exit 1
        fi
        print_success "js-framework-benchmark: $JS_BENCHMARK_DIR"
    fi
}

# =============================================================================
# Micro-Benchmark Functions
# =============================================================================

run_micro_benchmarks() {
    print_header "Running Micro-Benchmarks (BenchmarkDotNet)"
    
    print_warning "Remember: Micro-benchmarks may show false positives!"
    print_warning "Always validate with E2E benchmarks before merging."
    echo ""
    
    cd "$ROOT_DIR"
    mkdir -p "$RESULTS_DIR/micro"
    
    local filter="*"
    local job_arg=""
    
    if $QUICK_MODE; then
        print_info "Running in QUICK mode (fewer iterations)"
        job_arg="--job short"
    fi
    
    # Run DOM Diffing benchmarks
    print_info "Running DOM Diffing benchmarks..."
    dotnet run --project Abies.Benchmarks -c Release -- \
        --filter '*DomDiffingBenchmarks*' \
        --exporters json \
        --artifacts "$RESULTS_DIR/micro/diffing" \
        $job_arg || true
    
    # Run Keyed Diffing benchmarks
    print_info "Running Keyed Diffing benchmarks..."
    dotnet run --project Abies.Benchmarks -c Release -- \
        --filter '*KeyedDiffingBenchmarks*' \
        --exporters json \
        --artifacts "$RESULTS_DIR/micro/keyed" \
        $job_arg || true
    
    # Run Rendering benchmarks
    print_info "Running Rendering benchmarks..."
    dotnet run --project Abies.Benchmarks -c Release -- \
        --filter '*RenderingBenchmarks*' \
        --exporters json \
        --artifacts "$RESULTS_DIR/micro/rendering" \
        $job_arg || true
    
    # Run Event Handler benchmarks
    print_info "Running Event Handler benchmarks..."
    dotnet run --project Abies.Benchmarks -c Release -- \
        --filter '*EventHandlerBenchmarks*' \
        --exporters json \
        --artifacts "$RESULTS_DIR/micro/handlers" \
        $job_arg || true
    
    print_success "Micro-benchmarks complete!"
    print_info "Results saved to: $RESULTS_DIR/micro/"
}

# =============================================================================
# E2E Benchmark Functions
# =============================================================================

build_abies_for_benchmark() {
    print_info "Building Abies for benchmark..."
    
    cd "$JS_BENCHMARK_DIR/frameworks/keyed/abies/src"
    
    # Clean and rebuild
    rm -rf bin obj
    dotnet publish -c Release
    
    # Copy to bundled-dist
    rm -rf ../bundled-dist/*
    mkdir -p ../bundled-dist
    cp -R bin/Release/net10.0/publish/wwwroot/* ../bundled-dist/
    
    print_success "Abies built and copied to bundled-dist"
}

start_benchmark_server() {
    print_info "Starting benchmark server..."
    
    cd "$JS_BENCHMARK_DIR"
    
    # Kill any existing server on port 8080
    lsof -ti:8080 | xargs kill -9 2>/dev/null || true
    
    # Start server in background
    npm start &
    SERVER_PID=$!
    
    # Wait for server to start
    sleep 3
    
    # Check if server is running
    if ! curl -s http://localhost:8080 > /dev/null; then
        print_error "Failed to start benchmark server"
        exit 1
    fi
    
    print_success "Server running on http://localhost:8080 (PID: $SERVER_PID)"
}

stop_benchmark_server() {
    print_info "Stopping benchmark server..."
    lsof -ti:8080 | xargs kill -9 2>/dev/null || true
    print_success "Server stopped"
}

run_e2e_benchmarks() {
    print_header "Running E2E Benchmarks (js-framework-benchmark)"
    
    print_info "This is the SOURCE OF TRUTH for performance decisions."
    echo ""
    
    # Build Abies
    build_abies_for_benchmark
    
    # Start server
    start_benchmark_server
    trap stop_benchmark_server EXIT
    
    cd "$JS_BENCHMARK_DIR/webdriver-ts"
    
    mkdir -p "$RESULTS_DIR/e2e"
    
    # Run key benchmarks
    print_info "Running 01_run1k (create 1000 rows)..."
    npm run bench -- --headless --framework abies-keyed --benchmark 01_run1k || true
    
    print_info "Running 05_swap1k (swap two rows)..."
    npm run bench -- --headless --framework abies-keyed --benchmark 05_swap1k || true
    
    print_info "Running 09_clear1k (clear all rows)..."
    npm run bench -- --headless --framework abies-keyed --benchmark 09_clear1k || true
    
    # Copy results
    cp results/abies*.json "$RESULTS_DIR/e2e/" 2>/dev/null || true
    
    print_success "E2E benchmarks complete!"
    print_info "Results saved to: $RESULTS_DIR/e2e/"
    
    # Stop server
    stop_benchmark_server
    trap - EXIT
}

compare_e2e_results() {
    print_header "Comparing E2E Results Against Baseline"
    
    cd "$ROOT_DIR"
    
    if [[ ! -f "$BASELINE_FILE" ]]; then
        print_warning "No baseline file found at: $BASELINE_FILE"
        print_info "Run with --update-baseline to create one"
        return
    fi
    
    python3 scripts/compare-benchmark.py \
        --results-dir "$RESULTS_DIR/e2e" \
        --baseline "$BASELINE_FILE" \
        --threshold 5.0 \
        --framework abies
}

update_e2e_baseline() {
    print_header "Updating E2E Baseline"
    
    cd "$ROOT_DIR"
    
    if [[ ! -d "$RESULTS_DIR/e2e" ]]; then
        print_error "No E2E results found. Run --e2e first."
        exit 1
    fi
    
    python3 scripts/compare-benchmark.py \
        --results-dir "$RESULTS_DIR/e2e" \
        --baseline "$BASELINE_FILE" \
        --framework abies \
        --update-baseline
    
    print_success "Baseline updated: $BASELINE_FILE"
}

# =============================================================================
# Summary Functions
# =============================================================================

print_summary() {
    print_header "Benchmark Summary"
    
    echo "Results location: $RESULTS_DIR"
    echo ""
    
    if $RUN_MICRO; then
        echo "📊 Micro-benchmarks:"
        echo "   - DOM Diffing: $RESULTS_DIR/micro/diffing/"
        echo "   - Keyed Diffing: $RESULTS_DIR/micro/keyed/"
        echo "   - Rendering: $RESULTS_DIR/micro/rendering/"
        echo "   - Event Handlers: $RESULTS_DIR/micro/handlers/"
        echo ""
    fi
    
    if $RUN_E2E; then
        echo "🎯 E2E benchmarks (Source of Truth):"
        echo "   - Results: $RESULTS_DIR/e2e/"
        echo "   - Baseline: $BASELINE_FILE"
        echo ""
    fi
    
    echo -e "${YELLOW}═══════════════════════════════════════════════════════════════${NC}"
    echo -e "${YELLOW}  REMINDER: Benchmarking Strategy${NC}"
    echo -e "${YELLOW}═══════════════════════════════════════════════════════════════${NC}"
    echo ""
    echo "  1. Micro-benchmarks are for DEVELOPMENT FEEDBACK only"
    echo "  2. E2E benchmarks are the SOURCE OF TRUTH"
    echo "  3. NEVER ship based on micro-benchmark improvements alone"
    echo ""
    echo "  See: docs/investigations/benchmarking-strategy.md"
    echo ""
}

# =============================================================================
# Main
# =============================================================================

main() {
    # Parse arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            --micro)
                RUN_MICRO=true
                shift
                ;;
            --e2e)
                RUN_E2E=true
                shift
                ;;
            --all)
                RUN_MICRO=true
                RUN_E2E=true
                shift
                ;;
            --quick)
                QUICK_MODE=true
                shift
                ;;
            --compare)
                COMPARE_MODE=true
                shift
                ;;
            --update-baseline)
                UPDATE_BASELINE=true
                RUN_E2E=true
                shift
                ;;
            --help|-h)
                show_help
                ;;
            *)
                print_error "Unknown option: $1"
                show_help
                ;;
        esac
    done
    
    # Default to micro if nothing specified
    if ! $RUN_MICRO && ! $RUN_E2E; then
        print_info "No benchmark type specified, defaulting to --micro"
        RUN_MICRO=true
    fi
    
    # Check prerequisites
    check_prerequisites
    
    # Create results directory
    mkdir -p "$RESULTS_DIR"
    
    # Run benchmarks
    if $RUN_MICRO; then
        run_micro_benchmarks
    fi
    
    if $RUN_E2E; then
        run_e2e_benchmarks
        
        if $COMPARE_MODE; then
            compare_e2e_results
        fi
        
        if $UPDATE_BASELINE; then
            update_e2e_baseline
        fi
    fi
    
    # Print summary
    print_summary
}

main "$@"
