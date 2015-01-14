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
            | Constant t -> 
                Observable.Return t
            | Varying property -> property.Observable

    let value (p : Property<'T>) = p.Value

    let onNextWith f (p : Property<'T>) = p.OnNextWith f

    let onNext p = onNextWith (fun _ -> true) p

    let exposeStream (p : Property<'T>) = p.ExposeStream

    let returnC t = Constant t
     
    let returnV initial obs =
        property initial obs
        |> Varying

    let fmap f =
        fun p -> 
            match p with
            | Constant v -> 
                Constant <| f v
            | Varying _ -> 
                let newValue = f <| value p
                let newStream = exposeStream p |> Observable.map f
                returnV newValue newStream

    let bind (property : Property<'a>) (fn : ('a -> Property<'b>)) : Property<'b> =
        fn (value property)

    let combine (p1 : Property<'a>) (p2 : Property<'b>) = 
        p2

    let inline (>>=) p f = bind p f

module Builders =

    open Property

    type PropertyBuilder() = 

        member __.Return c = returnC c

        member __.ReturnFrom (p : Property<'T>) = p

        member __.Bind (p : Property<'a>, f : ('a -> Property<'b>)) =
            p >>= f

        member __.Combine (p1 : Property<'a>, p2 : Property<'b>) = 
            combine p1 p2

        member __.Zero() = failwith "Zero"

    let propertyB = PropertyBuilder()

    type PropertyFmapBuilder() = 

        member __.Bind (p : Property<'a>, f: 'a -> Property<'b>) =
            value (Property.fmap f p) //todo
             
        member __.ReturnFrom (p : Property<'T>) = p

        member __.Zero() = failwith "Zero"

    let propertyFmapBuilder = PropertyFmapBuilder()