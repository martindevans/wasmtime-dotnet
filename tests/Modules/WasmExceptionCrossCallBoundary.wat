(module
	(import "env" "cs_test" (func $cs_test (result i32)))
	(import "env" "cs_test_catch" (func $cs_test_catch (result i32)))
	(import "env" "cs_test_rethrow" (func $cs_test_rethrow (result i32)))

	(tag $my_error (export "my_error") (param i32))

	;; Mimics C# import env:test
	;; Test function, to sanity check expected case without C# call boundary.
	(func $wat_test (result i32)
		(call $throw)
		;; Below is unreachable. It's here to mimic C#, which must return something.
		(i32.const 222)
		(return)
	)

	;; Function to thrown from C#
	(func $throw (export "throw") (result i32)
		(i32.const 111)
		(throw $my_error)
		(unreachable)
	)

	(func $run_cs (export "run_cs") (result i32)
		(block $handler (result i32)
			(try_table (catch $my_error $handler)
				;; We do or call something that throws;
				;; Call into C#, making a C# calling boundary.
				(call $cs_test)
				(return)
			)
			;; Prior methods should always throw,
			;; or they'll return for some reason, in which case we've already returned that.
			(unreachable)
		)

		;; If the catch was triggered, we end up right here.
		;; Since all other paths in the block return (or is unreachable), this means the catch caught it.

		;; However, for sanity, we are going to return the result of the block.
		;; If it works; We'll return (111), which is what $throw gives to $my_error tag when throwing. 
		;; If C# catches it; We'll get (222).
		;; Otherwise we'll get unreachable or C# will get WasmException

		(return)
	)

	(func $run_cs_catch (export "run_cs_catch") (result i32)
        (call $cs_test_catch)
        (return)
	)

	(func $run_cs_rethrow (export "run_cs_rethrow") (result i32)
		(block $handler (result i32)
			(try_table (catch $my_error $handler)
				(call $cs_test_rethrow)
				(return)
			)
			(unreachable)
		)

		(return)
	)

	(func $run_wat (export "run_wat") (result i32)
		(block $handler (result i32)
			(try_table (catch $my_error $handler)
				(call $wat_test)
				(return)
			)
			(unreachable)
		)

		(return)
	)
)
