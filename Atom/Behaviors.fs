namespace Atom

open System
open System.Collections.Generic
open FSharp.Control.Reactive
open System.Reactive.Disposables
open System.Reactive.Linq

(*
    http://conal.net/papers/icfp97/icfp97.pdf
*)

module Property =

    type CoreProperty<'T> (observable: IObservable<'T>, initial) =

        let mutable value = initial

        let obs = 
            observable 
            |> Observable.publish 
            |> Observable.refCount

        let subDisposable = 
            obs
            |> Observable.subscribe (fun v -> value <- v)

        member this.Observable = obs

        member this.Value
            with get () = value

        interface IDisposable with
            member this.Dispose () = subDisposable.Dispose()

    let property (initial : 'T) obs =
        new CoreProperty<'T> (obs, initial)

    type Property<'T> = 
        private
        | Constant of 'T
        | Varying of CoreProperty<'T>
         
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

    let value (b : Property<'T>) = b.Value

    let onNextWith f (b : Property<'T>) = b.OnNextWith f

    let onNext b = onNextWith (fun _ -> true) b

    let exposeStream (b : Property<'T>) = b.ExposeStream

    let returnC t = Constant t
     
    let returnV initial obs =
        property initial obs
        |> Varying

    let fmap f =
        fun b -> 
            match b with
            | Constant v -> 
                Constant <| f v
            | Varying _ -> 
                let newValue = f <| value b
                let newStream = exposeStream b |> Observable.map f
                returnV newValue newStream

    let bind (behavior : Property<'a>) (fn : ('a -> Property<'b>)) : Property<'b> =
        fn (value behavior)

    let combine (b1 : Property<'a>) (b2 : Property<'b>) = 
        b2

    let inline (>>=) b f = bind b f

module Builders =

    open Property

    type PropertyBuilder() = 

        member __.Return b = returnC b

        member __.ReturnFrom (b : Property<'T>) = b

        member __.Bind (b : Property<'a>, f : ('a -> Property<'b>)) =
            b >>= f

        member __.Combine (b1 : Property<'a>, b2 : Property<'b>) = 
            combine b1 b2

        member __.Zero() = failwith "Zero"

    let behaviorB = PropertyBuilder()

    type PropertyFmapBuilder() = 

        member __.Bind (b : Property<'a>, f: 'a -> Property<'b>) =
            value (Property.fmap f b) //todo
             
        member __.ReturnFrom (b : Property<'T>) = b

        member __.Zero() = failwith "Zero"

    let behaviorFmapBuilder = PropertyFmapBuilder()