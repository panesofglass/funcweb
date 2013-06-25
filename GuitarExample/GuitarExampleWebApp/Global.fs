namespace FsWeb

open System.Net.Http
open System.Web.Http
open Frank

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
module Api =
    open System
    open System.Net
    open System.Net.Http
    open Frank
    open Guitars

    (** Guitar API **)
    let getGuitars (request: HttpRequestMessage) = async {
        return request.CreateResponse(HttpStatusCode.OK, getGuitars())
    }

    let postGuitar (request: HttpRequestMessage) = async {
        let! guitarData = request.Content.AsyncReadAs<Newtonsoft.Json.Linq.JToken>()
        //let! guitar = request.Content.AsyncReadAs<Guitar>()
        let guitar = Guitar(Name = (guitarData.SelectToken("name") |> string))
        match addGuitar guitar with
        | Some() ->
            let response = request.CreateResponse(HttpStatusCode.Created, guitar)
            let location = "/guitar/" + guitar.Name
            response.Headers.Location <- Uri(request.RequestUri, location)
            return response
        | None ->
            return request.CreateErrorResponse(HttpStatusCode.BadRequest, "You must supply a name.")
    }

    let guitarsResource = route "guitars" (get getGuitars <|> post postGuitar)


    (** Guitar API **)
    let getId request =
        match getParam request "id" with
        | Some(name) -> Some(string name)
        | None -> None

    let getGuitar (request: HttpRequestMessage) = async {
        match getId request with
        | Some(name) ->
            match Guitars.getGuitar name with
            | Some(guitar) ->
                return request.CreateResponse(HttpStatusCode.OK, guitar)
            | None ->
                return request.CreateResponse(HttpStatusCode.NotFound)
        | None ->
            return request.CreateResponse(HttpStatusCode.NotFound)
    }

    let deleteGuitar (request: HttpRequestMessage) = async {
        match getId request with
        | Some(name) ->
            match removeGuitar name with
            | Some() -> 
                return request.CreateResponse(HttpStatusCode.NoContent)
            | None ->
                return request.CreateResponse(HttpStatusCode.NotFound)
        | None ->
            return request.CreateResponse(HttpStatusCode.NotFound)
    }

    let guitarResource = route "guitar/{id}" (get getGuitar <|> delete deleteGuitar)



(**
 * Run the app in ASP.NET
 *)
type Global() =
    inherit System.Web.HttpApplication() 

    member this.Start() =
        let config = GlobalConfiguration.Configuration
        config
        |> Frank.Core.register [ Api.guitarsResource; Api.guitarResource ]
        |> ignore

        config.Formatters.JsonFormatter.SerializerSettings.ContractResolver <-
            Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
