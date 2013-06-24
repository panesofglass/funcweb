namespace FsWeb.Controllers

open System
open System.IO
open System.Net
open System.Net.Http
open System.Web.Http
open FsWeb.Models

type LocationLink = { controller: string
                      id: string }

type GuitarsController() =    
    inherit ApiController()    

    let guitars = if File.Exists @"c:\temp\Guitars.txt" then
                      File.ReadAllText(@"c:\temp\Guitars.txt").Split(',') 
                      |> Array.map (fun x -> Guitar(Name = x))
                  else [||]

    member this.Get() = guitars

    member this.Get(id) =
        match guitars |> Array.tryFind (fun g -> g.Name = id) with
        | Some(guitar) ->
            this.Request.CreateResponse(HttpStatusCode.OK, guitar)
        | None ->
            this.Request.CreateResponse(HttpStatusCode.NotFound)

    member this.Post(guitar: Guitar) =
        let isNameValid = not (String.IsNullOrEmpty(guitar.Name))

        match this.ModelState.IsValid, isNameValid with
        | true, true ->
            let result = guitars |> Array.fold(fun acc x -> acc + x.Name + ",") ""
            File.WriteAllText(@"c:\temp\Guitars.txt", result + guitar.Name)

            let location = this.Url.Link("DefaultApi",
                                         { controller = this.ControllerContext.ControllerDescriptor.ControllerName
                                           id = guitar.Name })
            let response = this.Request.CreateResponse(HttpStatusCode.Created, guitar)
            response.Headers.Location <- Uri(location)
            response

        | false, _ | _, false ->
            let response = this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "You must supply a name.")
            raise <| HttpResponseException(response)

    member this.Delete(id) =
        match guitars |> Array.tryFindIndex(fun g -> g.Name = id) with
        | Some(index) ->
            let result = String.Join(",", guitars |> Array.filter (fun g -> g.Name <> id))
            File.WriteAllText(@"c:\temp\Guitars.txt", result)
            this.Request.CreateResponse(HttpStatusCode.NoContent)
        | None ->
            this.Request.CreateResponse(HttpStatusCode.NotFound)
