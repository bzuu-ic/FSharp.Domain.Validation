![nuget](https://img.shields.io/nuget/v/FSharp.ValidationBlocks.svg?style=badge&logo=appveyor&color=brightgreen)

# <img style="border-radius: 8%;" width="64" height="64" src="https://raw.githubusercontent.com/lfr/FSharp.ValidationBlocks/master/FSharp.ValidationBlocks.png"> <big>FSharp.ValidationBlocks</big>


A tiny library with huge potential to simplify and streamline your domain design, as you can see from the examples below:

| <center>Without ValidationBlocks</center> | <center>With ValidationBlocks</center> |
|---|---|
|<pre>// Single-case union style<br>type Tweet = private Tweet of string<br>module Tweet =<br>  let validate = function<br>  &#124; s when String.IsNullOrWhitespace s →<br>     IsMissingOrBlank &#124;&gt; Error<br>  &#124; s when s.Length > 280 →<br>     IsTooLong 280 &#124;&gt; Error<br>  &#124; s → Tweet s &#124;&gt; Ok<br>  let value (x:Tweet) =<br>     let (Tweet s) = x in s<br><br>// Object-oriented style<br>type Tweet private (s) =<br>   static member Validate s = function<br>   &#124; s when String.IsNullOrWhitespace s →<br>      IsMissingOrBlank &#124;&gt; Error<br>   &#124; s when s.Length > 280 →<br>      IsTooLong 280 &#124;&gt; Error<br>   &#124; s → Tweet s &#124;&gt; Ok<br>   interface IConstrained&lt;string&gt; with<br>      member x.Value = s</pre>|<pre>type Tweet = private Tweet of Text with<br>   interface IText with<br>      member _.Validate =<br>         fun s → s.Length > 280 => IsTooLong 280</pre>|

You may have noticed that the examples on the left have an additional validation case. On the right this validation is implicit in the statement that a `Tweet` is a `Tweet of Text`. Since validation blocks are built on top of each other, the only rules that need to be explicitly declared are the rules <u>specific to the block itself</u>. One could imagine a similar behavior with OO-style types, but there's no simple way to achieve with private constructors.

## Interface? Really?

F# is a multi-paradigm language. Regardless of whether you think it's a good thing or a bad thing (it's both), with the right discipline certain OO concepts can be harnessed for their expressiveness without any of the baggage. For instance here we use *interface* as an elegant way in a single statement to both:
* Identify a type as a ValidationBlock
* Enforce the definition of validation rules

There will otherwise be **no mentions** of interfaces in the code that uses and creates validation blocks, only when you declare the block in your domain definition file.

## How it works

First you declare your error types, then you declare your actual domain types (i.e. `Tweet`), and finally you use them with the provided `Block.value` and `Block.validate` functions.

### Declaring your errors

Before declaring types like the one above, you do need define your error type. This can be a brand new validation-specific discriminated union or part of an existing one.

```fsharp
type TextError =
    | ContainsControlCharacters
    | ContainsTabs
    | IsTooLong of int
    | IsMissingOrBlank
```

While not strictly necessary, the next single line of code greatly improves the readability of your type declarations simply by abbreviating the `IBlock<_,_>` interface for a specific primitive type.

```fsharp
// all string blocks can now interface IText instead of IBlock<string, TextError>
type IText = inherit IBlock<string, TextError>
```

### Declaring ValidationBlocks
Type declaration is reduced to the absolute minimum. A type is given a name, a private constructor, and the interface above that essentially makes it a **ValidationBlock** and ensures that you define the validation rule.

The  validation rule is a function of the primitive type (`string` here) that returns a list of one or more errors depending on the stated conditions.

```fsharp
/// Single or multi-line non-null non-blank text without any additional validation
type FreeText = private FreeText of string with
    interface IText with
        member _.Validate =
            // validation rule
            fun s ->
                [if s |> String.IsNullOrWhiteSpace then IsMissingOrBlank]
```

### Using operators to further simplify type declarations
The type declaration above can be simplified further using the provided `=>` and `==>` **validation operators** that here combine a predicate of `string` with an error true.

```fsharp
/// Alternative type declaration using the ==> operator
type FreeText = private FreeText of string with
    interface IText with
        member _.Validate =
            // validation rule using validation operators
            String.IsNullOrWhiteSpace ==> IsMissingOrBlank
```
To use operators make sure to open `FSharp.ValidationBlocks.Operators` in the file(s) where you declare your ValidationBlocks. See [Text.fs](/lfr/FSharp.ValidationBlocks/blob/master/src/Example/Text.fs) for more examples of validation operators.

### Creating and using blocks in your code

Using blocks is very easy, let's say you have a block binding called `email`, you can simply access its value using the following:

```fsharp
// get the primitive value from the block
Block.value email // → string
```

There's also an experimental operator `%` that essentially does the same thing:

```fsharp
// experimental — same as Block.value
%email // → string
```

Creating a block is just as simple:

```fsharp
// create a block when block type can be inferred
Block.validate s // → Ok 'block | Error e
```

When type inference isn't possible, specify block type using the generic parameter:

```fsharp
// create a block when block type can be inferred
Block.validate<Tweet> s // → Ok Tweet | Error e
```

Do **not** force type inference using type annotations as it's unnecessarily verbose:

```fsharp
// incorrect example, do not copy/paste
let email : Result<Email, TextError list> =     // :(
    Block.validate "incorrect@dont.do"

// correct alternative
let email =
    Block.validate<Email> "cartermp@fsharp.lang" // :)
```

In both cases the resulting `email` is of type `Result<Email, TextError list>`.

## Exceptions instead of Error
The `Block.validate` method returns a `Result`, which may not always be necessary, for instance when de-serializing values that are guaranteed to be valid, you can just use:

```fsharp
// throws an exception if not valid
Unchecked.blockof "this better be valid"
// same as above without type inference
Unchecked.blockof<Text> "this better be valid 2"
```

## Serialization

There's a `System.Text.Json.Serialization.JsonConverter` included, if you add it to your serialization options all blocks are serialized to (and de-serialized from) their primitive type. It is good practice to keep your serialized content independent from implementation considerations such as ValidationBlocks.

## Not just strings

Strings are the perfect example as it's usually the first type for which developers stitch together validation logic, but this library works with anything, you can create a `PositiveInt` that's guaranteed to be greater than zero, or a `FutureDate` that's guaranteed to not be in the past. Lists, vectors, any type of object really, if you can write a predicate against it, you can validate it. They're 100% generic so the sky is the limit.

## Ok looks good, but I'm still not sure

I've created a checklist to help the decision of using this library:

&#x2705; My project contains domain objects or records

If all of the above then this library is for you. It's tiny, doesn't have any dependencies, and uses F# concepts in the way they're meant to be used, so if one day you decide to no longer use it, you can simply get rid of it and still keep all the single-case unions that you've defined. All you'll need to do is create your own implementation of `Block.validate` and `Block.value` or just make their constructors public. Finally, if you use the provided JsonConverter, it ensures that your blocks are not serialized as ValidationBlocks, so you're not adding any indirect dependency between this library and whatever is on the other side of your serialization.

## Conclusion

Using validation blocks you can create airtight domain objects guaranteed to never have invalid content. Not only you're writing less code, but your domain code files are much smaller and nicer to work with. You'll also get [ROP](https://fsharpforfunandprofit.com/rop/) almost for free, and while there is a case to be made [against ROP](https://fsharpforfunandprofit.com/posts/against-railway-oriented-programming/), it's definitely a perfect match for content validation, especially content that may be entered by a user.

Tweet [@fishyrock](https://twitter.com/fishyrock) to contribute or give feedback!

### Full working example
You can find a full working example in the file [Text.fs](/lfr/FSharp.ValidationBlocks/blob/master/src/Example/Text.fs)