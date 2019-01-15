module Tests

open Expecto
open Giraffe
open Giraffe.GoodRead
open System
open System.IO
open System.Linq
open Microsoft.AspNetCore.Http
open System.Collections.Generic
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.TestHost
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open System.Net
open System.Threading.Tasks
open System.Net.Http
open Expecto.CSharp
open Moq;
open Microsoft.Extensions.Logging
open Newtonsoft.Json
open Giraffe.Serialization.Json

let jsonSerializer = Fable.Remoting.Json.FableJsonConverter() 
let serialize (x: 'a) =  JsonConvert.SerializeObject(x, jsonSerializer)

let toJson (x: 'a) = setBodyFromString (serialize x)
let fromJson<'t>(input: string) = JsonConvert.DeserializeObject<'t>(input, jsonSerializer)

type User = { Id: int; Username: string }

type IUserStore =
    abstract getAll : unit -> Async<User list>
    abstract getById : int -> Async<Option<User>>


module Users =
   
    let getById userId =
        require {
            let! logger = service<ILogger>()
            let! userStore = service<IUserStore>()
            return async {
                match! Async.Try (userStore.getById userId) with
                | Ok (Some user) ->
                    return toJson (Ok user)
                | Ok None ->
                    return setStatusCode 404 >=> toJson (Error "User was not found")
                | Error ex ->
                    do logger.LogError(ex, "Error while searching user")
                    return setStatusCode 500 >=> toJson (Error "Internal server error occured")
            }
        }

    let getAll() =
        require {
            let! logger = service<ILogger>()
            let! userStore = service<IUserStore>()
            return async {
                match! Async.Try (userStore.getAll()) with
                | Ok users -> return toJson users
                | Error err ->
                    logger.LogError(err, "Error while getting all users")
                    return setStatusCode 500 >=> toJson (Error "Internal server error occured")
            }
        }

let pass() = Expect.isTrue true "Passed"
let fail() = Expect.isTrue false "Failed"

let appBuilder (webApp: HttpHandler) =
  fun  (app: IApplicationBuilder) -> app.UseGiraffe webApp

let configureServices (services: IServiceCollection) =
  services.AddGiraffe()
  |> ignore

let createHost (webApp: HttpHandler) (setupServices: IServiceCollection -> IServiceCollection) =
    WebHostBuilder()
        .UseContentRoot(Directory.GetCurrentDirectory())
        .Configure(Action<IApplicationBuilder> (appBuilder webApp))
        .ConfigureServices(Action<IServiceCollection> (fun services ->
            services.AddGiraffe() |> ignore
            setupServices services |> ignore))

let runTask task =
    task
    |> Async.AwaitTask
    |> Async.RunSynchronously

let httpGet (path : string) (client : HttpClient) =
    path
    |> client.GetAsync
    |> runTask

let isStatus (code : HttpStatusCode) (response : HttpResponseMessage) =
    Expect.equal response.StatusCode code "Status code is wrong"
    response

let ensureSuccess (response : HttpResponseMessage) =
    if not response.IsSuccessStatusCode
    then response.Content.ReadAsStringAsync() |> runTask |> failwithf "%A"
    else response

let readText (response : HttpResponseMessage) =
    response.Content.ReadAsStringAsync()
    |> runTask

let readTextEqual content (response : HttpResponseMessage) =
    response.Content.ReadAsStringAsync()
    |> runTask
    |> fun result -> Expect.equal result content "The expected and actual response content are not equal"

type TestUserStore(users) =
    interface IUserStore with
        member this.getAll() = async { return users }

        member this.getById userId =
            async { return List.tryFind (fun user -> user.Id = userId) users }

[<Tests>]
let tests =

    testList "Giraffe.GoodRead" [
    
        testCase "Dependencies are correctly resolved" <| fun () ->
            let webApp = GET >=> route "/users" >=> Require.apply (Users.getAll())
            let logger = Mock.Of<ILogger>()
            let emptyUserList = [ ]
            let emptyUserStore = TestUserStore(emptyUserList)
        
            let setup (services: IServiceCollection) =
                services.AddSingleton<ILogger>(logger)
                        .AddSingleton<IUserStore>(emptyUserStore)
        
            let server = new TestServer(createHost webApp setup)
            let client = server.CreateClient()
        
            client
            |> httpGet "/users"
            |> isStatus HttpStatusCode.OK
            |> readText
            |> fromJson<User list>
            |> fun users -> Expect.isEmpty users "Users list should be empty"
    ]
