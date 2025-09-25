function initializeDynCalls() {
  Module.dynCall_vi = Module.dynCall_vi || ((cb, arg1) => getWasmTableEntry(cb)(arg1));
  Module.dynCall_vii = Module.dynCall_vii || ((cb, arg1, arg2) => getWasmTableEntry(cb)(arg1, arg2))
  Module.dynCall_viii = Module.dynCall_viii || ((cb, arg1, arg2, arg3) => getWasmTableEntry(cb)(arg1, arg2, arg3))
  Module.dynCall_viiii = Module.dynCall_viiii || ((cb, arg1, arg2, arg3, arg4) => getWasmTableEntry(cb)(arg1, arg2, arg3, arg4))
}
Module.preRun.push(() => {
    initializeDynCalls();
});