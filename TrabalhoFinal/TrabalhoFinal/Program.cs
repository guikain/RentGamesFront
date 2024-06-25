
using Models.Game;
using RentGames.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

class Program{
    public static void Main(String[] args) {

        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        	
        var origins = "_origins";
        
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(name: origins,
            policy =>
                {
                    policy.WithOrigins("http://localhost:3000")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
        });
        



       //conectando com o bando de dados!
       builder.Services.AddDbContext<AppDbContext>();
        

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApiDocument(config =>
        {
            config.DocumentName = "RentAGame";
            config.Title = "RentGames";
            config.Version = "v1";
        });
        
        WebApplication app = builder.Build();
        if (app.Environment.IsDevelopment())
        {
            app.UseOpenApi();
            app.UseSwaggerUi(config =>
            {
                config.DocumentTitle = "RentGames API";
                config.Path = "/swagger";
                config.DocumentPath = "/swagger/{documentName}/swagger.json";
                config.DocExpansion = "list";
            });
        }

        app.UseCors(origins);

        //meus endpoints!

        //1
        app.MapGet("/games", ([FromServices] AppDbContext context) => {
            List<Game> games = context.Games.ToList();
            return games.Any() ? Results.Ok(games) : Results.NotFound();
        }).Produces<List<Game>>();

        //2
        app.MapGet("/games/promo", ([FromServices] AppDbContext context) => {
            List<Game> promos = context.Games.Where(g => g.IsPromo).ToList();
            return promos.Any() ? Results.Ok(promos) : Results.NotFound("Não foi localizada nenhuma promoção.");
        }).Produces<List<Game>>();

        //3
        app.MapPost("/games/register", ([FromServices] AppDbContext context, [FromBody] Game game) => {

            List<string> errorMessages = new List<string>();
            if (game.Price <= 0 || game.Price > 10000)
                errorMessages.Add("O preço deve estar entre 0.01 e 10000.00.");
            if (string.IsNullOrEmpty(game.Name))
                errorMessages.Add("O nome do jogo é obrigatório.");
            else if (game.Name.Length > 100)
                errorMessages.Add("O nome do jogo não pode exceder 100 caracteres.");


            if (errorMessages.Any())
                return Results.BadRequest(new { Errors = errorMessages });


            context.Games.Add(game);
            context.SaveChanges();
            return Results.Created($"/games/{game.Id}", game);
        }).Produces<Game>();

        //4
        app.MapGet("/games/{id}", ([FromServices] AppDbContext context, [FromRoute] Guid id) => {
            var game = context.Games.FirstOrDefault(g => g.Id == id);
            return game != null ? Results.Ok(game) : Results.NotFound("Jogo não encontrado.");
        }).Produces<Game>();
        

        //5
        app.MapPut("/games/set/{id}", ([FromServices] AppDbContext context, [FromRoute] Guid id, [FromBody] Game updatedGame) => {
            var existingGame = context.Games.FirstOrDefault(g => g.Id == id);
            if (existingGame == null) {
                return Results.NotFound("Jogo não encontrado.");
            }
            Game newGame = existingGame with {
                Id = id,
                Price = updatedGame.Price,
                Name = updatedGame.Name,
                IsPromo = updatedGame.IsPromo,
                IsStock = updatedGame.IsStock
            };
            context.Games.Remove(existingGame);
            context.Games.Add(newGame);
            context.SaveChanges();

            return Results.Ok(newGame);
        }).Produces<Game>();

        //6
        app.MapDelete("/games/delete/{id}", ([FromServices] AppDbContext context, [FromRoute] Guid id) => {
            var game = context.Games.FirstOrDefault(g => g.Id == id);
            if (game != null) {
                context.Games.Remove(game);
                context.SaveChanges();
                return Results.Ok(game);
            }
            return Results.NotFound("Jogo não encontrado.");
        }).Produces<Game>();


        //7
        app.MapPatch("/games/setstock/{id}", ([FromServices] AppDbContext context, [FromRoute] Guid id, [FromBody] bool isStock) => {
            var game = context.Games.Find(id);
            if (game == null) {
                return Results.NotFound("Jogo não encontrado.");
            }
            
            var updatedGame = game with { IsStock = isStock };
            context.Entry(game).CurrentValues.SetValues(updatedGame);
            context.SaveChanges();
            
            return Results.Ok(updatedGame);
        }).Produces<Game>();

        app.Run();
    }
}