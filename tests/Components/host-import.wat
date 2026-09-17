;; Component importing a "host" instance, used to test host function definitions.
;;
;; The core start function calls "transform" during instantiation, exercising both callback
;; argument and result handling. "greet" is only imported, so it must still be defined too.
(component
  (import "host" (instance $h
    (export "transform" (func (param "x" s32) (result s32)))
    (export "greet" (func (param "name" string) (result string)))))
  (alias export $h "transform" (func $transform))
  (core func $transform_core (canon lower (func $transform)))
  (core module $m
    (import "host" "transform" (func $transform (param i32) (result i32)))
    (func (export "run") (param i32) (result i32)
      local.get 0
      call $transform)
    (func $start
      i32.const 21
      call $transform
      i32.const 42
      i32.ne
      if
        unreachable
      end)
    (start $start)
  )
  (core instance $i (instantiate $m
    (with "host" (instance (export "transform" (func $transform_core))))))
  (func (export "run") (param "x" s32) (result s32)
    (canon lift (core func $i "run")))
)
