#!/usr/bin/env python3
"""
P3 -- the over-match control (architect ruling 10-architect-ruling-classifier.md,
Q-G): a corpus of commands that merely MENTION git/gh under a DATA_ONLY_HEADS
head must produce ZERO exit-2 outcomes, so the inversion's cost (Q-A's
deny-by-default) stays measured rather than assumed.

"Per the enforcement-inventory-discipline rules, derive this corpus from
DATA_ONLY_HEADS x a small argument set rather than writing its members down;
a hand-listed corpus perishes the moment the list changes." -- this script
extracts DATA_ONLY_HEADS directly from the hook's own source (never a copy
hand-maintained here), so growing/shrinking that list automatically grows/
shrinks this corpus with it.

Usage:
  python3 p3_overmatch.py <hook_path> <repo_dir>
"""
import ast
import json
import os
import re
import subprocess
import sys

ARG_TEMPLATES = [
    '{head} "git commit -m x"',
    '{head} git',
    '{head} gh',
    '{head} "git push origin main"',
    '{head} -n "git commit"',
    '{head} "some git text" "and gh too"',
]


def extract_data_only_heads(hook_path):
    with open(hook_path) as f:
        content = f.read()
    start = content.index("cat <<'PY'") + len("cat <<'PY'") + 1
    end = content.rindex("\nPY\n")
    py_src = content[start:end]
    tree = ast.parse(py_src)
    for node in ast.walk(tree):
        if isinstance(node, ast.Assign) and len(node.targets) == 1 \
           and isinstance(node.targets[0], ast.Name) and node.targets[0].id == "DATA_ONLY_HEADS":
            value = ast.literal_eval(node.value)
            return set(value)
    raise RuntimeError("could not find DATA_ONLY_HEADS in %s" % hook_path)


def main():
    hook_path, repo_dir = sys.argv[1], sys.argv[2]
    heads = extract_data_only_heads(hook_path)
    if not heads:
        print("P3: DATA_ONLY_HEADS is empty -- nothing to derive a corpus from")
        sys.exit(0)

    corpus = []
    for head in sorted(heads):
        for tmpl in ARG_TEMPLATES:
            corpus.append(tmpl.format(head=head))

    exit2 = []
    for cmd in corpus:
        payload = json.dumps({"cwd": repo_dir, "tool_name": "Bash", "tool_input": {"command": cmd}})
        env = dict(os.environ)
        env["CLAUDE_PROJECT_DIR"] = repo_dir
        try:
            r = subprocess.run(["bash", hook_path], input=payload, capture_output=True,
                                text=True, env=env, timeout=15)
        except subprocess.TimeoutExpired:
            exit2.append((cmd, "TIMEOUT"))
            continue
        if r.returncode != 0:
            exit2.append((cmd, (r.stdout + r.stderr).strip()))

    print("P3: %d DATA_ONLY_HEADS members, %d corpus commands, %d exit-2 (want 0)"
          % (len(heads), len(corpus), len(exit2)))
    for cmd, out in exit2[:30]:
        print("  EXIT-NONZERO: %r" % cmd)
        if out:
            print("                %s" % out[:200])
    sys.exit(1 if exit2 else 0)


if __name__ == "__main__":
    main()
