// using System;
// using System.Collections.Generic;
// using System.Data;
// using System.Net.Http;
// using System.Text.Json;
// using System.Threading.Tasks;
// using Microsoft.AspNetCore.Mvc;
// using Oracle.ManagedDataAccess.Client;

// namespace PokemonApi.Controllers
// {
//     [ApiController]
//     [Route("api/[controller]")]
//     public class PokemonController : ControllerBase
//     {
//         private readonly string _connectionString = "User Id=USUARIO;Password=SENHA;Data Source=HOST:PORT/SERVICE_NAME";
//         private readonly HttpClient _httpClient;

//         public PokemonController(IHttpClientFactory httpClientFactory)
//         {
//             _httpClient = httpClientFactory.CreateClient();
//         }

//         // POST api/pokemon
//         [HttpPost]
//         public async Task<IActionResult> InserirPokemon([FromBody] PokemonRequest request)
//         {
//             if (string.IsNullOrWhiteSpace(request.Nome))
//             {
//                 return BadRequest(new { erro = "Nome do Pokémon é obrigatório" });
//             }

//             // Consulta à PokeAPI
//             string url = $"https://pokeapi.co/api/v2/pokemon/{request.Nome.ToLower()}";
//             HttpResponseMessage response = await _httpClient.GetAsync(url);

//             if (!response.IsSuccessStatusCode)
//             {
//                 return NotFound(new { erro = "Pokémon não encontrado na PokeAPI" });
//             }

//             var json = await response.Content.ReadAsStringAsync();
//             using JsonDocument doc = JsonDocument.Parse(json);
//             JsonElement root = doc.RootElement;

//             // Exemplo: usaremos o primeiro tipo como "cor" do Pokémon
//             string nomeCor = "desconhecido";
//             if (root.TryGetProperty("types", out JsonElement types) && types.GetArrayLength() > 0)
//             {
//                 nomeCor = types[0].GetProperty("type").GetProperty("name").GetString();
//             }

//             try
//             {
//                 // Inserir ou recuperar a cor e inserir o Pokémon
//                 int idCor = InserirOuRecuperarCor(nomeCor);
//                 InserirPokemonNoBanco(request.Nome, idCor);
//             }
//             catch (Exception ex)
//             {
//                 return StatusCode(500, new { erro = "Erro ao inserir dados no banco", detalhe = ex.Message });
//             }

//             return Created("", new { mensagem = $"{request.Nome} inserido com a cor {nomeCor}" });
//         }

//         // GET api/pokemon
//         [HttpGet]
//         public IActionResult ListarPokemons()
//         {
//             var pokemons = new List<PokemonResponse>();

//             using (OracleConnection conn = new OracleConnection(_connectionString))
//             {
//                 conn.Open();
//                 string query = @"
//                     SELECT p.NOME_POKEMON, c.NOME_COR
//                     FROM POKEMONS p
//                     JOIN CORES c ON p.ID_COR = c.ID_COR
//                 ";

//                 using (OracleCommand cmd = new OracleCommand(query, conn))
//                 {
//                     using OracleDataReader reader = cmd.ExecuteReader();
//                     while (reader.Read())
//                     {
//                         pokemons.Add(new PokemonResponse
//                         {
//                             Nome = reader["NOME_POKEMON"].ToString(),
//                             Cor = reader["NOME_COR"].ToString()
//                         });
//                     }
//                 }
//             }

//             return Ok(pokemons);
//         }

//         // Método para inserir ou recuperar a cor
//         private int InserirOuRecuperarCor(string nomeCor)
//         {
//             int idCor = 0;
//             using (OracleConnection conn = new OracleConnection(_connectionString))
//             {
//                 conn.Open();
//                 // Verifica se a cor já existe
//                 string selectQuery = "SELECT ID_COR FROM CORES WHERE NOME_COR = :nome";
//                 using (OracleCommand cmd = new OracleCommand(selectQuery, conn))
//                 {
//                     cmd.Parameters.Add(new OracleParameter("nome", nomeCor));
//                     object result = cmd.ExecuteScalar();
//                     if (result != null)
//                     {
//                         idCor = Convert.ToInt32(result);
//                         return idCor;
//                     }
//                 }

//                 // Se não existir, insere a nova cor
//                 string insertQuery = "INSERT INTO CORES (NOME_COR) VALUES (:nome)";
//                 using (OracleCommand cmd = new OracleCommand(insertQuery, conn))
//                 {
//                     cmd.Parameters.Add(new OracleParameter("nome", nomeCor));
//                     cmd.ExecuteNonQuery();
//                 }
//                 conn.Commit();

//                 // Recupera o ID da cor inserida
//                 using (OracleCommand cmd = new OracleCommand(selectQuery, conn))
//                 {
//                     cmd.Parameters.Add(new OracleParameter("nome", nomeCor));
//                     object result = cmd.ExecuteScalar();
//                     if (result != null)
//                     {
//                         idCor = Convert.ToInt32(result);
//                     }
//                 }
//             }
//             return idCor;
//         }

//         // Método para inserir o Pokémon no banco
//         private void InserirPokemonNoBanco(string nomePokemon, int idCor)
//         {
//             using (OracleConnection conn = new OracleConnection(_connectionString))
//             {
//                 conn.Open();
//                 string insertQuery = "INSERT INTO POKEMONS (NOME_POKEMON, ID_COR) VALUES (:nome, :id_cor)";
//                 using (OracleCommand cmd = new OracleCommand(insertQuery, conn))
//                 {
//                     cmd.Parameters.Add(new OracleParameter("nome", nomePokemon));
//                     cmd.Parameters.Add(new OracleParameter("id_cor", idCor));
//                     cmd.ExecuteNonQuery();
//                 }
//                 conn.Commit();
//             }
//         }
//     }

//     // Model para requisição
//     public class PokemonRequest
//     {
//         public string Nome { get; set; }
//     }

//     // Model para resposta
//     public class PokemonResponse
//     {
//         public string Nome { get; set; }
//         public string Cor { get; set; }
//     }
// }
