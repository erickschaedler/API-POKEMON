using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace API_POKEMON.Controllers;

[ApiController]
[Route("[controller]")]
public class PokemonController1 : ControllerBase
{
    public class APokemonResult
    {
        public string? name { get; set; }
    }

    public class AResponseGetPokemonInPokeApiDto
    {
        public List<APokemonResult>? results { get; set; }
    }

    [HttpGet]
    [Route("pokemonsGroupedByColor/{quantity:int}")]
    public async Task<ActionResult> GetPokemonsGroupedByColorAsync (int quantity)
    {
        try
        {
            if (quantity < 1 || quantity > 100)
                throw new ArgumentException("Informe uma quantidade entre 1 e 100");

            using var client = new HttpClient();
            var baseApi = "https://pokeapi.co/api/v2/";

            var response = await client.GetAsync($"{baseApi}pokemon?limit={quantity}&offset=0");
            if(!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Erro ao consultar API de pokemons: {baseApi}");

            var responseContent = await response.Content.ReadAsStringAsync();
            var deserializedContent = JsonSerializer.Deserialize<AResponseGetPokemonInPokeApiDto>(responseContent)
                ?? throw new Exception("Erro ao deserializar resposta da API de pokemons");

            var results = deserializedContent.results;

            if(results == null || !results.Any())
                throw new Exception("Objeto `results` que contém os pokemosn está inválido");

            var tasks = results.Select<APokemonResult, Task<(string Name, string Color, string Error)>>(
                async (APokemonResult pokemon) => 
                {
                    var pokemonName = pokemon.name;
                    if(string.IsNullOrWhiteSpace(pokemonName))
                        return (Name: null, Color: null, Error)
                });
        }
        catch(ArgumentException aex)
        {
            return BadRequest(aex.Message);
        }
        catch(HttpRequestException hre)
        {
            return BadRequest(hre.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Erro interno ao consultar os pokemons: {ex.Message}");
        }
    }
}