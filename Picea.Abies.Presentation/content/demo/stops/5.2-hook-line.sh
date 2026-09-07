try:
    d = json.loads(sys.stdin.read())
    if not isinstance(d, dict):
        raise ValueError("payload is not a JSON object")
    agent = (d.get("agent_type") or "").strip()
    tool = (d.get("tool_name") or "").strip()
    cwd = os.path.normpath(d.get("cwd") or os.getcwd())
    tool_input = d.get("tool_input") or {}
    if not isinstance(tool_input, dict):
        raise ValueError("tool_input is not a JSON object")
except Exception:
    print("UNREADABLE")
    sys.exit(0)

rules = DENY.get(agent)
