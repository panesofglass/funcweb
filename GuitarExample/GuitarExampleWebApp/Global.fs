﻿namespace FsWeb

open System.Net.Http
open System.Web.Http

type Guitar() = 
    member val Name = "" with get, set

(**
 * Define Guitar catalog operations
 *)
module Guitars =
    open System
    open System.IO

    let getGuitars() =
        if File.Exists @"c:\temp\Guitars.txt" then
            File.ReadAllText(@"c:\temp\Guitars.txt").Split(',') 
            |> Array.map (fun x -> Guitar(Name = x))
        else [||]

    let getGuitar name =
        getGuitars() |> Array.tryFind(fun g -> g.Name = name)

    let addGuitar (guitar: Guitar) =
        if not (String.IsNullOrEmpty(guitar.Name)) then
            let result = getGuitars() |> Array.fold(fun acc x -> acc + x.Name + ",") ""
            File.WriteAllText(@"c:\temp\Guitars.txt", result + guitar.Name)
            Some()
        else None

    let removeGuitar name =
        let guitars = getGuitars()
        match guitars |> Array.tryFindIndex(fun g -> g.Name = name) with
        | Some(_) ->
            let result = String.Join(",", guitars |> Array.filter (fun g -> g.Name <> name))
            File.WriteAllText(@"c:\temp\Guitars.txt", result)
            Some()
        | None -> None


(**
 * Expose APIs
 *)


(**
 * Run the app in ASP.NET
 *)
type Global() =
    inherit System.Web.HttpApplication() 

    member this.Start() =
        let config = GlobalConfiguration.Configuration
        config
        |> HttpResource.register [ (* Add APIs here *) ]
        |> ignore

        config.Formatters.JsonFormatter.SerializerSettings.ContractResolver <-
            Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
