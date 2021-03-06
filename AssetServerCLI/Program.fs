﻿open System
open System.IO
open System.Threading
open System.Threading.Tasks
open System.Security.Permissions
open System.Net
open System.Text

open WebSocketSharp
open WebSocketSharp.Server

open System.Diagnostics
open FSharp.Control.Reactive

let httpListener assetPath (handler:(string -> HttpListenerRequest -> HttpListenerResponse -> Async<unit>)) =
    let hl = new HttpListener()
    hl.Prefixes.Add "http://*:8080/"
    hl.Start ()
    async {
        while true do
            let! context = Async.FromBeginEnd (hl.BeginGetContext, hl.EndGetContext)
            Async.Start (handler assetPath context.Request context.Response)
    } |> Async.Start

let serveAsset assetPath (request:HttpListenerRequest) (response:HttpListenerResponse) = async {
    try
        let filePath = assetPath + request.Url.AbsolutePath
        let fileInfo = new FileInfo (filePath)
        use fs = new FileStream (filePath, FileMode.Open)
        response.ContentType <- Mime.MediaTypeNames.Application.Octet
        response.ContentLength64 <- fileInfo.Length
        let stream = response.OutputStream
        do! Async.AwaitIAsyncResult (fs.CopyToAsync response.OutputStream) |> Async.Ignore
    with e ->
        response.ContentLength64 <- 0L
        printfn "%A" e.Message
    }

let httpHandler assetPath (request:HttpListenerRequest) (response:HttpListenerResponse) = async {
        printfn "httpHandler GET %A" request.Url.AbsolutePath
        do! serveAsset assetPath request response
        response.OutputStream.Close () }

type WebSocketAsset =
    inherit WebSocketBehavior

    val fileObserver : IObservable<string>
    val mutable fileHandler : IDisposable

    new (fileObserver) = {
        fileObserver = fileObserver
        fileHandler = null }

    member this.Cast (msg:string) = this.Send ("fswatch:" + msg)

    override this.OnOpen () =
        printfn "WebSocket Open"
        this.fileHandler <-
            this.fileObserver
            |> Observable.subscribe this.Cast

    override this.OnClose (e: CloseEventArgs) =
        printfn "WebSocket Close %A" e
        if this.fileHandler <> null then
            this.fileHandler.Dispose ()

    override this.OnMessage (e:MessageEventArgs) = printfn "WebSocket Message %A" e

let webSocketServer fileObserver =
//    let ws = new WebSocketServer ("ws://192.168.4.110:8081")
    let ws = new WebSocketServer ("ws://localhost:8081")
    ws.KeepClean <- false
    ws.AddWebSocketService<WebSocketAsset> ("/asset", fun () -> new WebSocketAsset (fileObserver) )
    ws.Start ()

[<EntryPoint>]
let main argv = 

    let assetPath = System.IO.Path.GetFullPath ("../../../Assets")
    printfn "pwd %A" (Directory.GetCurrentDirectory ())
    printfn "assetPath %A" assetPath

    let ps = new ProcessStartInfo ("fswatch", "-r " + assetPath)
    ps.UseShellExecute <- false
    ps.RedirectStandardOutput <- true
    let proc = Process.Start ps

    let fileObserver =
        proc.OutputDataReceived
        |> Observable.map (fun evt -> evt.Data.Replace (assetPath + "/", ""))

    fileObserver
    |> Observable.subscribe (fun msg -> printfn "Proc: %A" msg)
    |> ignore

    proc.BeginOutputReadLine ()

    httpListener assetPath httpHandler
    webSocketServer fileObserver

    printfn "Press return to exit..."
    Console.ReadLine () |> ignore

    proc.Kill ()
    0

