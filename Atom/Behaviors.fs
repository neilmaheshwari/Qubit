namespace Atom

open System
open System.Collections.Generic
open FSharp.Control.Reactive
open System.Reactive.Disposables
open System.Reactive.Linq
open Nessos.FsPickler
open Nessos.FsPickler.Combinators
open Atom

(*
    http://conal.net/papers/icfp97/icfp97.pdf
*)


module Behaviors =

    type Behavior<'T> = 
        | Constant of 'T
        | Varying of Property<'T>
         
        member x.Value = 
            match x with
            | Constant t -> t
            | Varying property -> property.Value

        member x.OnNextWith f = 
            match x with
            | Constant t -> t
            | Varying property ->
                property.Observable
                |> Observable.filteri (fun _ i -> i = 1)
                |> Observable.filter f
                |> Observable.First                    

        member x.ExposeStream =
            match x with
            | Constant t -> Observable.Repeat t
            | Varying property -> property.Observable

    let value (b : Behavior<'T>) = b.Value

    let onNextWith f (b : Behavior<'T>) = b.OnNextWith f

    let onNext b = onNextWith (fun _ -> true) b

    let exposeStream (b : Behavior<'T>) = b.ExposeStream

    let returnC t = Constant t

    let returnV obs = 
        let property = Observable.property (Observable.First obs) obs
        let b = Varying property
        b |> exposeStream |> Observable.subscribe ignore |> ignore
        b

    let fmap f =
        fun b -> 
            match b with
            | Constant _ -> 
                Constant (f (value b))
            | Varying _ -> 
                let newStream = exposeStream b |> Observable.map f
                returnV newStream

    let bind (behavior : Behavior<'a>) (fn : ('a -> Behavior<'b>)) : Behavior<'b> =
        fn (value behavior)

    let combine (b1 : Behavior<'a>) (b2 : Behavior<'b>) = 
        b2

    let inline (>>=) b f = bind b f

module Builders =

    open Behaviors

    type BehaviorBuilder() = 

        member __.Return b = returnC b

        member __.ReturnFrom (b : Behavior<'T>) = b

        member __.Bind (b : Behavior<'a>, f : ('a -> Behavior<'b>)) =
            b >>= f

        member __.Combine (b1 : Behavior<'a>, b2 : Behavior<'b>) = 
            combine b1 b2

        member __.Zero() = failwith "Zero"

    let behaviorB = BehaviorBuilder()