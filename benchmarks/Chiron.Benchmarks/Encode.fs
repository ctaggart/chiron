namespace ChironB.Benchmarks

open Chiron
open BenchmarkDotNet.Attributes
open System.Text

module Examples =
    module Inline =
        module Explicit =
            module E = Chiron.Serialization.Json.Encode
            module JO = Chiron.JsonObject
            type Testing =
                { one: int option
                  two: bool
                  three: int }
                static member Encode (x: Testing, jObj: JsonObject): JsonObject =
                    jObj
                    |> JO.writeOptionalWith E.int "1" x.one
                    |> JO.writeWith E.bool "2" x.two
                    |> JO.writeWith E.int "3" x.three
            let testObject = { one = None; two = true; three = 42 }

        module Inferred =
            module E = Chiron.Serialization.Json.Encode
            module JO = Chiron.JsonObject
            module JO = Chiron.Inference.JsonObject
            type Testing =
                { one: int option
                  two: bool
                  three: int }
                static member Encode (x: Testing, jObj: JsonObject): JsonObject =
                    jObj
                    |> JO.writeOptional "1" x.one
                    |> JO.write "2" x.two
                    |> JO.write "3" x.three
                static member ToJson (x: Testing): Json =
                    Testing.Encode (x, JsonObject.empty)
                    |> Chiron.Inference.Json.encode
            let testObject = { one = None; two = true; three = 42 }

    module InModule =
        module Explicit =
            module E = Chiron.Serialization.Json.Encode
            module JO = Chiron.JsonObject
            type Testing =
                { one: int option
                  two: bool
                  three: int }
            module Testing =
                let encode x jObj =
                    jObj
                    |> JO.writeOptionalWith E.int "1" x.one
                    |> JO.writeWith E.bool "2" x.two
                    |> JO.writeWith E.int "3" x.three
            type Testing with
                static member Encode (x: Testing, jObj: JsonObject): JsonObject =
                    Testing.encode x jObj
            let testObject = { one = None; two = true; three = 42 }

        module Inferred =
            module E = Chiron.Serialization.Json.Encode
            module JO = Chiron.JsonObject
            module JO = Chiron.Inference.JsonObject
            type Testing =
                { one: int option
                  two: bool
                  three: int }
            module Testing =
                let encode x jObj =
                    jObj
                    |> JO.writeOptional "1" x.one
                    |> JO.write "2" x.two
                    |> JO.write "3" x.three
            type Testing with
                static member ToJson (x: Testing): Json =
                    JsonObject.buildWith Testing.encode x
            let testObject = { one = None; two = true; three = 42 }

    module Obsolete =
        open ChironObsolete
        module ComputationExpression =
            type Testing =
                { one: int option
                  two: bool
                  three: int }
                static member ToJson (x: Testing): Json<unit> = json {
                    do! Json.writeUnlessDefault "1" None x.one
                    do! Json.write "2" x.two
                    do! Json.write "3" x.three
                }
            let testObject = { one = None; two = true; three = 42 }

        module Operators =
            open ChironObsolete.Operators
            type Testing =
                { one: int option
                  two: bool
                  three: int }
                static member ToJson (x: Testing): Json<unit> =
                       Json.writeUnlessDefault "1" None x.one
                    *> Json.write "2" x.two
                    *> Json.write "3" x.three
            let testObject = { one = None; two = true; three = 42 }

[<Config(typeof<CoreConfig>)>]
type Encoding () =
    [<Benchmark>]
    member x.Inline_Explicit () =
        Inference.Json.encodeObject Examples.Inline.Explicit.testObject

    [<Benchmark>]
    member x.InModule_Explicit () =
        Inference.Json.encodeObject Examples.InModule.Explicit.testObject

    [<Benchmark>]
    member x.Inline_Inferred () =
        Inference.Json.encode Examples.Inline.Inferred.testObject

    [<Benchmark>]
    member x.InModule_Inferred () =
        Inference.Json.encode Examples.InModule.Inferred.testObject

    [<Benchmark(Baseline=true)>]
    member x.Version6_ComputationExpression () =
        ChironObsolete.Mapping.Json.serialize Examples.Obsolete.ComputationExpression.testObject

    [<Benchmark>]
    member x.Version6_Operators () =
        ChironObsolete.Mapping.Json.serialize Examples.Obsolete.Operators.testObject

// module Method1 =
//     open Chiron.ObjectReader.Operators

//     type ComplexType =
//         { a: int list option
//           b: ChildType
//           c: MixinType }
//     and ChildType =
//         { d: WrapperType
//           e: byte array }
//     and MixinType =
//         { f: int list }
//     and [<Struct>] WrapperType =
//         | Sad of string

//     module Encoders =
//         module WrapperType =
//             let toJson (Sad x) = Json.ofString x
//             let fromJson = Optic.get Json.Optics.String_ >> Result.map Sad
//         module MixinType =
//             let mk f = { f = f }
//             let encode x = JsonObject.add "f" (Json.ofListWith Json.ofInt32 x.f)
//             let decode = mk <!> JsonObject.read "f"
//         module ChildType =
//             let mk d e = { d = d; e = e }
//             let encode x =
//                 JsonObject.add "d" (WrapperType.toJson x.d)
//              >> JsonObject.add "e" (Json.ofBytes x.e)
//             let decode =
//                 mk
//                 <!> JsonObject.readWith WrapperType.fromJson "d"
//                 <*> JsonObject.read "e"
//         module ComplexType =
//             let mk a b c = { a = a; b = b; c = c }
//             let encode x =
//                 JsonObject.writeOptWith (Json.ofListWith Json.ofInt32) "a" x.a
//              >> JsonObject.writeObjWith (ChildType.encode) "b" x.b
//              >> JsonObject.mixinObj (MixinType.encode) x.c
//             let decode =
//                 mk
//                 <!> JsonObject.readOpt "a"
//                 <*> (JsonObject.tryGetOrFail "b" >> Result.bind (Optic.get Json.Optics.Object_) >> Result.bind ChildType.decode)
//                 <*> MixinType.decode

//     type ComplexType with
//         static member Encode x = Encoders.ComplexType.encode x
//         static member Decode (_:ComplexType) = Encoders.ComplexType.decode

//     type ChildType with
//         static member Encode x = Encoders.ChildType.encode x
//         static member Decode (_:ChildType) = Encoders.ChildType.decode

//     type MixinType with
//         static member Encode x = Encoders.MixinType.encode x
//         static member Decode (_:MixinType) = Encoders.MixinType.decode

//     type WrapperType with
//         static member ToJson x = Encoders.WrapperType.toJson x
//         static member FromJson (_:WrapperType) = Encoders.WrapperType.fromJson

//     module Constants =
//         let expected =
//             { a = Some [ 2; 4; 6; 8 ]
//               b = { d = Sad "winter"
//                     e = "Hello world!" |> System.Text.Encoding.UTF8.GetBytes }
//               c = { f = [ 1; 2; 3; 4 ] } }

// module Method2 =
//     open Chiron.ObjectReader.Operators

//     type ComplexType =
//         { a: int list option
//           b: ChildType
//           c: MixinType }
//     and ChildType =
//         { d: WrapperType
//           e: byte array }
//     and MixinType =
//         { f: int list }
//     and [<Struct>] WrapperType =
//         | Sad of string

//     type WrapperType with
//         static member ToJson (Sad x) = Json.ofString x
//         static member FromJson (_:WrapperType) = Optic.get Json.Optics.String_ >> Result.map Sad

//     type MixinType with
//         static member Encode x = JsonObject.add "f" (Json.ofListWith Json.ofInt32 x.f)
//         static member Decode (_:MixinType) = (fun f -> { f = f }) <!> JsonObject.read "f"

//     type ChildType with
//         static member Encode x =
//             JsonObject.add "d" (WrapperType.ToJson x.d)
//             >> JsonObject.add "e" (Json.ofBytes x.e)
//         static member Decode (_:ChildType) =
//             (fun d e -> { d = d; e = e })
//             <!> JsonObject.readWith (WrapperType.FromJson Unchecked.defaultof<_>) "d"
//             <*> JsonObject.read "e"

//     type ComplexType with
//         static member Encode x =
//             JsonObject.writeOptWith (Json.ofListWith Json.ofInt32) "a" x.a
//             >> JsonObject.writeObjWith (ChildType.Encode) "b" x.b
//             >> JsonObject.mixinObj (MixinType.Encode) x.c
//         static member Decode (_:ComplexType) =
//             (fun a b c -> { a = a; b = b; c = c })
//             <!> JsonObject.readOpt "a"
//             <*> (JsonObject.tryGetOrFail "b" >> Result.bind (Optic.get Json.Optics.Object_) >> Result.bind (ChildType.Decode Unchecked.defaultof<_>))
//             <*> (MixinType.Decode Unchecked.defaultof<_>)

//     module Constants =
//         let expected =
//             { a = Some [ 2; 4; 6; 8 ]
//               b = { d = Sad "winter"
//                     e = "Hello world!" |> System.Text.Encoding.UTF8.GetBytes }
//               c = { f = [ 1; 2; 3; 4 ] } }

// module Method3 =
//     open Chiron.ObjectReader.Operators

//     let readObjWith (reader : ObjectReader<'a>) k jsonObject : JsonResult<'a> =
//         JsonObject.tryGetOrFail k jsonObject
//         |> Result.bind (Optic.get Json.Optics.Object_)
//         |> Result.bind reader

//     let inline readObjInfer k jsonObject =
//         let decoder = (^a : (static member Decode : ObjectReader<'a>) ())
//         readObjWith decoder k jsonObject

//     let writeObjWith (writer: ObjectWriter<'a>) (k: string) (v: 'a) (jsonObject: JsonObject) : JsonObject =
//         let json = writer v JsonObject.empty |> JsonObject.toJson
//         JsonObject.add k json jsonObject

//     let inline writeObjWithDefault (defaults : ^def) (k : string) (v : ^a) (jsonObject : JsonObject) =
//         let json = ((^a or ^def) : (static member Encode : ^a -> JsonObject -> JsonObject) (v, jsonObject)) |> JsonObject.toJson
//         JsonObject.add k json jsonObject

//     let inline writeObj (k : string) (v : ^a) (jsonObject : JsonObject) =
//         writeObjWithDefault ChironDefaults k v jsonObject

//     let inline mixinObjInfer v jObj =
//         (^a : (static member Encode : 'a -> JsonObject -> JsonObject) (v, jObj))

//     type ComplexType =
//         { a: int list option
//           b: ChildType
//           c: MixinType }
//     and ChildType =
//         { d: WrapperType
//           e: byte array }
//     and MixinType =
//         { f: int list }
//     and [<Struct>] WrapperType =
//         | Sad of string

//     module WrapperType =
//         let toJson (Sad x) = Json.ofString x
//         let fromJson = Optic.get Json.Optics.String_ >> Result.map Sad
//     type WrapperType with
//         static member ToJson x = WrapperType.toJson x
//         static member FromJson (_:WrapperType) = WrapperType.fromJson

//     module MixinType =
//         let mk f = { f = f }
//         let encode x jobj = jobj |> JsonObject.write "f" x.f
//         let decode = mk <!> JsonObject.read "f"
//     type MixinType with
//         static member Encode (x, jobj) = MixinType.encode x jobj
//         static member Decode = MixinType.decode

//     module ChildType =
//         let mk d e = { d = d; e = e }
//         let encode x jobj =
//             jobj
//             |> JsonObject.write "d" x.d
//             |> JsonObject.write "e" x.e
//         let decode =
//             mk
//             <!> JsonObject.read "d"
//             <*> JsonObject.read "e"
//     type ChildType with
//         static member Encode (x, jobj) = ChildType.encode x jobj
//         static member Decode = ChildType.decode

//     module ComplexType =
//         let mk a b c = { a = a; b = b; c = c }
//         let encode x jobj =
//             jobj
//             |> JsonObject.writeOpt "a" x.a
//             |> writeObj "b" x.b
//             |> mixinObjInfer x.c
//         let decode =
//             mk
//             <!> JsonObject.readOpt "a"
//             <*> readObjInfer "b"
//             <*> MixinType.decode
//     type ComplexType with
//         static member Encode (x, jobj) = ComplexType.encode x jobj
//         static member Decode = ComplexType.decode

//     module Constants =
//         let expected =
//             { a = Some [ 2; 4; 6; 8 ]
//               b = { d = Sad "winter"
//                     e = "Hello world!" |> System.Text.Encoding.UTF8.GetBytes }
//               c = { f = [ 1; 2; 3; 4 ] } }

// module Constants =
//     let thing = """{"f":[1,2,3,4],"a":[2,4,6,8],"b":{"e":"SGVsbG8gd29ybGQh","d":"winter"}}"""
//     let thing2 = """{"a":[2,4,6,8],"b":{"d":"winter","e":"SGVsbG8gd29ybGQh"},"f":[1,2,3,4]}"""

// [<Config(typeof<CoreConfig>)>]
// type Decode () =
//     let jObj = Json.parseOrThrow Constants.thing |> Optic.get Json.Optics.Object_ |> function | Ok x -> x; | Error _ -> failwithf "Nope"

//     [<Benchmark>]
//     member __.Prebuild () : JsonResult<Method1.ComplexType> =
//         Method1.ComplexType.Decode Unchecked.defaultof<_> jObj

//     [<Benchmark>]
//     member __.PrebuildAlt () : JsonResult<Method3.ComplexType> =
//         Method3.ComplexType.Decode jObj

//     [<Benchmark(Baseline=true)>]
//     member __.NoPrebuild () : JsonResult<Method2.ComplexType> =
//         Method2.ComplexType.Decode Unchecked.defaultof<_> jObj

// [<Config(typeof<CoreConfig>)>]
// type Encode () =
//     [<Benchmark>]
//     member __.Prebuild () : Json =
//         JsonObject.build Method1.ComplexType.Encode Method1.Constants.expected

//     [<Benchmark>]
//     member __.PrebuildAlt () : Json =
//         Method3.ComplexType.Encode (Method3.Constants.expected, JsonObject.empty) |> JsonObject.toJson

//     [<Benchmark(Baseline=true)>]
//     member __.NoPrebuild () : Json =
//         JsonObject.build Method2.ComplexType.Encode Method2.Constants.expected
