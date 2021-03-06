﻿[<AutoOpen>]
module Types

open FSharp.Domain.Validation
open FSharp.Domain.Validation.Utils
open System


// 1. Define all the possible errors that the boxes can yield
type TextError =
    | ContainsControlCharacters
    | IsMissingOrBlank
    | IsNotAValidInteger


// 2. This interface in not strictly necessary but it makes
// type declarations and function signatures a more readable
type TextBlock = inherit IBox<string, TextError>


// 3. Define your custom types
/// Single or multi-line text without additional validation
type FreeText = private FreeText of string with
    interface TextBlock with
        member _.Validate =
            fun s ->
                [if String.IsNullOrWhiteSpace s then IsMissingOrBlank]

    // can be simplified using FSharp.Domain.Validation.Operators:
    // member _.Validate =
    //    String.IsNullOrWhiteSpace ==> ``Is missing or blank``
                            
/// Single line of text (no control characters)
type Text = private Text of FreeText with
    interface TextBlock with
        member _.Validate =
            fun s ->
                [if containsControlCharacters s then ContainsControlCharacters]

/// String representation of an integer
type Integer = private Integer of FreeText with
    interface TextBlock with
        member _.Validate =
            fun s ->
                [if Int32.TryParse(s) |> fst |> not then IsNotAValidInteger]




/// This type can be used to test that Fable's FSharp.Domain.Validation throws
/// exceptions with boxes of types that are discriminated unions,
/// test with: Block.validate<TestDu> (Ok "some text")
[<System.Obsolete>]
type TestDu = private TestDu of Result<string, string> with
    interface IBox<Result<string, string>, TextError> with
        member _.Validate = fun _ -> []

