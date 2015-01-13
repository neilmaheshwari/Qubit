module Cortex.Mario

open Atom
open Cortex
open OpenTK
open OpenTK.Graphics.ES30
open System
open FSharp.Control.Reactive

type FrontalEvents =
    | DeltaTimeEvent
    
let render = new RenderBuilder.Builder ()

let texture (tex:Texture.Texture2D) = RenderBuilder.State (fun interlude ->
    GL.ActiveTexture (TextureUnit.Texture0)
    tex.Bind ()
    interlude ()
    tex.Unbind ())

let shader = Shader.Vybe ()

let program = RenderBuilder.State (fun interlude ->
    shader.Bind ()
    interlude ()
    shader.Unbind ())

let shape (mesh:Shape.Mesh) = RenderBuilder.State (fun interlude ->
    shader.BindBuffers mesh.VBOs
    mesh.BindBuffers ()
    interlude ()
    mesh.UnbindBuffers ()
    shader.UnbindBuffers ())

let draw (mesh:Shape.Mesh) = RenderBuilder.State (fun interlude ->
    mesh.Draw ()
    interlude ())

let state f = RenderBuilder.State (fun interlude ->
    f ()
    interlude ())

let actor view proj =

    let animation =
        Axon.observe DeltaTimeEvent
        |> Observable.map (fun dt -> Matrix4.CreateRotationY dt)

    let model = animation |> Behaviors.property Matrix4.Identity

    model.Observable
    |> Observable.add (fun _ -> ())

    let parts = ["Mario/FitMario_BodyB"; "Mario/FitMario_BodyA"; "Mario/FitMario_EyeDmg"; "Mario/FitMario_Kage"]

    let meshes = List.fold (fun meshes part -> 
        (new Shape.Mesh (part), new Texture.Texture2D (part)) :: meshes ) [] parts
    
    let properties () = 
        shader.Model model.Value
        shader.View view
        shader.Proj proj

    render {

        let! s = program
        let! s = state properties

        for (mesh, tex) in meshes do
            let! t = texture tex
            let! b = shape mesh
            return! draw mesh
    }
