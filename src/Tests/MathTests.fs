module MathTests

open Expecto

// Simple math functions to test
module Math =
    let add x y = x + y

    let addMany numbers =
        List.fold (+) 0 numbers

    let addOption x yOpt =
        match yOpt with
        | Some y -> Some (x + y)
        | None -> None

[<Tests>]
let basicAdditionTests =
    testList "Basic Addition" [
        testCase "add two positive numbers" <| fun () ->
            let result = Math.add 2 3
            Expect.equal result 5 "2 + 3 should equal 5"

        testCase "add two negative numbers" <| fun () ->
            let result = Math.add -2 -3
            Expect.equal result -5 "-2 + -3 should equal -5"

        testCase "add positive and negative" <| fun () ->
            let result = Math.add 5 -3
            Expect.equal result 2 "5 + -3 should equal 2"

        testCase "add zero" <| fun () ->
            let result = Math.add 5 0
            Expect.equal result 5 "5 + 0 should equal 5"

        testCase "add with identity" <| fun () ->
            let result = Math.add 0 0
            Expect.equal result 0 "0 + 0 should equal 0"
    ]

[<Tests>]
let addManyTests =
    testList "Adding Multiple Numbers" [
        testCase "add empty list returns zero" <| fun () ->
            let result = Math.addMany []
            Expect.equal result 0 "Empty list should sum to 0"

        testCase "add single number" <| fun () ->
            let result = Math.addMany [42]
            Expect.equal result 42 "Single number should return itself"

        testCase "add multiple positive numbers" <| fun () ->
            let result = Math.addMany [1; 2; 3; 4; 5]
            Expect.equal result 15 "1+2+3+4+5 should equal 15"

        testCase "add mixed positive and negative" <| fun () ->
            let result = Math.addMany [10; -5; 3; -2]
            Expect.equal result 6 "10-5+3-2 should equal 6"

        testCase "add large list" <| fun () ->
            let numbers = [1..100]
            let result = Math.addMany numbers
            Expect.equal result 5050 "Sum of 1 to 100 should be 5050"
    ]

[<Tests>]
let addWithOptionTests =
    testList "Adding with Options" [
        testCase "add with Some value" <| fun () ->
            let result = Math.addOption 5 (Some 3)
            Expect.equal result (Some 8) "5 + Some 3 should equal Some 8"

        testCase "add with None returns None" <| fun () ->
            let result = Math.addOption 5 None
            Expect.equal result None "5 + None should equal None"
    ]

[<Tests>]
let propertyBasedTests =
    testList "Property-Based Addition Tests" [
        testProperty "addition is commutative" <| fun (x: int) (y: int) ->
            Math.add x y = Math.add y x

        testProperty "zero is identity element" <| fun (x: int) ->
            Math.add x 0 = x && Math.add 0 x = x

        testProperty "addition is associative" <| fun (x: int) (y: int) (z: int) ->
            Math.add (Math.add x y) z = Math.add x (Math.add y z)

        testProperty "addMany equals folding with +" <| fun (numbers: int list) ->
            Math.addMany numbers = List.fold (+) 0 numbers
    ]

[<Tests>]
let edgeCaseTests =
    testList "Edge Cases" [
        testCase "add maximum int values (overflow behavior)" <| fun () ->
            // F# int addition wraps on overflow
            let result = Math.add System.Int32.MaxValue 1
            Expect.equal result System.Int32.MinValue "Should wrap to MinValue"

        testCase "add minimum int values (underflow behavior)" <| fun () ->
            let result = Math.add System.Int32.MinValue -1
            Expect.equal result System.Int32.MaxValue "Should wrap to MaxValue"
    ]
