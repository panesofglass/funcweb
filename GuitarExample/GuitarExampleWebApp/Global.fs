namespace FsWeb

open System.Web.Http

type ApiRoute = { id: RouteParameter }

type Global() =
    inherit System.Web.HttpApplication() 

    static member RegisterApis(config: HttpConfiguration) =
        config.Routes.MapHttpRoute("DefaultApi",
                                   "{controller}/{id}",
                                   { id = RouteParameter.Optional }) |> ignore

        config.Formatters.JsonFormatter.SerializerSettings.ContractResolver <-
            Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()

    member this.Start() =
        Global.RegisterApis(GlobalConfiguration.Configuration)