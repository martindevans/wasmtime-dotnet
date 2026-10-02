;; Component importing host functions without invoking them.
(component
  (import "host" (instance $h
    (export "transform" (func (param "x" s32) (result s32)))
    (export "greet" (func (param "name" string) (result string)))))
)
