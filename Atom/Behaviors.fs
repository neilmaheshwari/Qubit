namespace Atom

open System
open System.Collections.Generic
open FSharp.Control.Reactive
open System.Reactive.Disposables
open System.Reactive.Linq
open Nessos.FsPickler
open Nessos.FsPickler.Combinators
open Atom

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

    let lift f (b : Behavior<'T>) =
        match b with
        | Constant _ -> Constant (f (value b))
        | Varying _ -> 
            let newStream = exposeStream b
                            |> Observable.map f
            let currentValue = value b
            let p' = new Property<'T> (newStream, currentValue)
            Varying p'

    
