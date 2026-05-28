(module
  ;; Define the tag locally
  (tag $my_error (param i32))
  
  ;; Import console.log
  (import "env" "log" (func $log (param i32)))

  (func $try_and_catch (param $value i32)
    (block $handler (result i32)
      ;; In try_table block, catch thrown exception
      (try_table (catch $my_error $handler)
        (call $might_throw (local.get $value))
      )
      (return)
    )
    ;; Log value returned by handler block
    call $log
  )

  ;; Function that throws an error of type $my_error
  ;; when its parameter is less than 0
  (func $might_throw (param $value i32)
    (local.get $value)
    (i32.const 0)
    (i32.lt_s)
    (if
      (then
        ;; Throw exception with payload of 42
        (i32.const 42)
        (throw $my_error)
      )
    )
  )

  (export "try_and_catch" (func $try_and_catch))
  (export "$might_throw" (func $might_throw))
)