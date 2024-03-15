from extism import host_fn, Plugin, set_log_file
import sys
import json

set_log_file("stdout", level='info')

@host_fn()
def myHostFunction1(input: str) -> str:
    print("Got input myHostFunction1: " + input)
    return "myHostFunction1: " + input

@host_fn()
def myHostFunction2(input: str) -> str:
    myobj = json.loads(input)
    print("Got input myHostFunction2: " + str(myobj))
    myobj["hello"] = "myHostFunction2"
    return json.dumps(myobj)

@host_fn()
def ubiqDispatch(input: str) -> str:
    myobj = json.loads(input)
    print("Got input ubiqDispatch: " + str(myobj))
    myobj["hello"] = "ubiqDispatch"
    return json.dumps(myobj)

with Plugin(open(sys.argv[1], "rb").read(), wasi=True) as plugin:
    result = plugin.call("ubiqEcho", b"Benjamin")
    print(result)