using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using TalkToApi.V1.Models;
using TalkToApi.V1.Repositories.Contracts;
using System.Security.Claims;
using TalkToApi.V1.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using AutoMapper;
using TalkToApi.Helpers.Contants;
using Microsoft.AspNetCore.Cors;

namespace TalkToApi.V1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [EnableCors("anyOrigin")]
    public class UsuarioController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userInManager;
        private readonly ITokenRepository _tokenrepository;
        public UsuarioController(IUsuarioRepository usuarioRepository,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userInManager,
            ITokenRepository tokenrepository, IMapper mapper)
        {
            _mapper = mapper;
            _usuarioRepository = usuarioRepository;
            _signInManager = signInManager;
            _userInManager = userInManager;
            _tokenrepository = tokenrepository;
        }
        [Authorize]
        [HttpGet("",Name ="UsuarioObterTodos")]
        [DisableCors]
        public ActionResult ObterTodos([FromHeader(Name = "Accept")]string mediaType)
        {
            var usuariosappuser = _userInManager.Users.ToList();
            if (mediaType == CustomMediaTypes.Hateoas)
            {
                var listaUsariosDTO = _mapper.Map<List<ApplicationUser>, List<UsuarioDTO>>(usuariosappuser);
                foreach (var lista in listaUsariosDTO)
                {
                    lista.Links.Add(new v1.Models.DTO.LinkDTO("self", Url.Link("UsuarioObter", new { id = lista.Id }), "GET"));
                }
                var listaDTO = new ListaDTO<UsuarioDTO>() { Lista = listaUsariosDTO };
                listaDTO.Links.Add(new v1.Models.DTO.LinkDTO("self", Url.Link("UsuarioObterTodos", null), "GET"));
                return Ok(listaDTO);
            }
            else
            {
                var usuarioresult = _mapper.Map<List<ApplicationUser>, List<UsuarioDTOSemHyperLink>>(usuariosappuser);
                return Ok(usuarioresult);
            }
        }
        [HttpGet("{id}",Name = "UsuarioObter")]
        [Authorize]
        public ActionResult ObterUsuario(string id, [FromHeader(Name = "Accept")]string mediaType)
        {
            var usuario = _userInManager.FindByIdAsync(id).Result;
            if (usuario == null)
                return NotFound();
            if (mediaType == CustomMediaTypes.Hateoas)
            {
                var usuarioDTO = _mapper.Map<ApplicationUser, UsuarioDTO>(usuario);
                usuarioDTO.Links.Add(new v1.Models.DTO.LinkDTO("self", Url.Link("UsuarioObter", new { id = usuarioDTO.Id }), "GET"));
                usuarioDTO.Links.Add(new v1.Models.DTO.LinkDTO("atualizar", Url.Link("UsuarioAtualizar", new { id = usuarioDTO.Id }), "PUT"));
                return Ok(usuarioDTO);
            }
            else
            {
                var usuarioresult = _mapper.Map<ApplicationUser, UsuarioDTOSemHyperLink>(usuario);
                return Ok(usuarioresult);
            }
        }

        [HttpPost("login")]
        public ActionResult Login([FromBody]UsuarioDTO usuariodto)
        {
            ModelState.Remove("ConfirmacaoSenha");
            ModelState.Remove("Nome");
            if (ModelState.IsValid)
            {
                ApplicationUser usuario = _usuarioRepository.Obter(usuariodto.Email, usuariodto.Senha);
                if (usuario != null)
                {
                    //login no identity
                    //   _signInManager.SignInAsync(usuario, false);
                    var token = BuildToken(usuario);
                    var tokenModel = new Token()
                    {
                        RefreshToken = token.RefrashToken,
                        ExpirationToken = token.Expiration,
                        ExpirationRefreshToken = token.ExpirationRefreshToken,
                        Usuario = usuario,
                        Criado = DateTime.Now,
                        Utilizado = false
                    };
                    _tokenrepository.Cadastrar(tokenModel);
                    return Ok(token);
                }
                else
                {
                    return NotFound("Úsuario não localizado");
                }
            }
            else
            {
                return UnprocessableEntity();
            }
        }

        [HttpPost("renovar")]
        public ActionResult Renovar([FromBody] TokenDTO tokenDTO)
        {
            var refreshtokendb = _tokenrepository.Obter(tokenDTO.RefrashToken);
            if (refreshtokendb == null)
            {
                return NotFound();
            }
            refreshtokendb.Atualizado = DateTime.Now;
            refreshtokendb.Utilizado = true;
            _tokenrepository.Atualizar(refreshtokendb);

            //GERAR novo token
            var usuario = _usuarioRepository.Obter(refreshtokendb.UsuarioId);
            var token = BuildToken(usuario);
            var tokenModel = new Token()
            {
                RefreshToken = token.RefrashToken,
                ExpirationToken = token.Expiration,
                ExpirationRefreshToken = token.ExpirationRefreshToken,
                Usuario = usuario,
                Criado = DateTime.Now,
                Utilizado = false
            };
            _tokenrepository.Cadastrar(tokenModel);
            return Ok(token);

        }

        [HttpPost("",Name ="UsuarioCadastrar")]
        public ActionResult Cadastrar([FromBody]UsuarioDTO usuarioDTO, [FromHeader(Name = "Accept")]string mediaType)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser usuario = _mapper.Map<UsuarioDTO,ApplicationUser>(usuarioDTO);
                
                var resultado = _userInManager.CreateAsync(usuario, usuarioDTO.Senha).Result;

                if (!resultado.Succeeded)
                {
                    List<string> erros = new List<string>();
                    foreach (var error in resultado.Errors)
                    {
                        erros.Add(error.Description);
                    }
                    return UnprocessableEntity(erros);
                }
                else
                {
                    if (mediaType == CustomMediaTypes.Hateoas)
                    {
                        var usuarioDTODB = _mapper.Map<ApplicationUser, UsuarioDTO>(usuario);
                        usuarioDTODB.Links.Add(new v1.Models.DTO.LinkDTO("self", Url.Link("UsuarioCadastrar", new { id = usuarioDTODB.Id }), "POST"));
                        usuarioDTODB.Links.Add(new v1.Models.DTO.LinkDTO("atualizar", Url.Link("UsuarioAtualizar", new { id = usuarioDTODB.Id }), "PUT"));
                        usuarioDTODB.Links.Add(new v1.Models.DTO.LinkDTO("obterUsuario", Url.Link("UsuarioObter", new { id = usuarioDTODB.Id }), "GET"));
                        return Ok(usuarioDTODB);
                    }
                    else
                    {
                        var usuarioresult = _mapper.Map<ApplicationUser, UsuarioDTOSemHyperLink>(usuario);
                        return Ok(usuarioresult);
                    }
                }
            }
            else
            {
                return UnprocessableEntity();
            }
        }
        [Authorize]
        [HttpPut("{id}",Name ="UsuarioAtualizar")]
        public ActionResult Atualizar(string id,[FromBody]UsuarioDTO usuarioDTO, [FromHeader(Name = "Accept")]string mediaType)
        {
            ApplicationUser usuario = _userInManager.GetUserAsync(HttpContext.User).Result;
            if (usuario.Id != id)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                usuario.FullName = usuarioDTO.Nome;
                usuario.UserName = usuarioDTO.Email;
                usuario.Email = usuarioDTO.Email;
                usuario.Slogan = usuarioDTO.Slogan;
                var resultado = _userInManager.UpdateAsync(usuario).Result;
                _userInManager.RemovePasswordAsync(usuario);
                _userInManager.AddPasswordAsync(usuario,usuarioDTO.Senha);

                if (!resultado.Succeeded)
                {
                    List<string> erros = new List<string>();
                    foreach (var error in resultado.Errors)
                    {
                        erros.Add(error.Description);
                    }
                    return UnprocessableEntity(erros);
                }
                else
                {
                    if (mediaType == CustomMediaTypes.Hateoas)
                    {
                        var usuarioDTODB = _mapper.Map<ApplicationUser, UsuarioDTO>(usuario);
                        usuarioDTODB.Links.Add(new v1.Models.DTO.LinkDTO("self", Url.Link("UsuaripAtualizar", new { id = usuarioDTODB.Id }), "PUT"));
                        usuarioDTODB.Links.Add(new v1.Models.DTO.LinkDTO("obterUsuario", Url.Link("UsuarioObter", new { id = usuarioDTODB.Id }), "GET"));
                        return Ok(usuarioDTODB);
                    }
                    else
                    {
                        var usuarioresult=_mapper.Map<ApplicationUser, UsuarioDTOSemHyperLink>(usuario);
                        return Ok(usuarioresult);
                    }
                }
            }
            else
            {
                return UnprocessableEntity();
            }
        }

        private TokenDTO BuildToken(ApplicationUser usuario)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Email,usuario.Email),
                new Claim(JwtRegisteredClaimNames.Sub,usuario.Id)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("chave-jwt-minhas-tarefas"));
            var sign = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var exp = DateTime.UtcNow.AddHours(1);

            JwtSecurityToken token = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: claims,
                expires: exp,
                signingCredentials: sign
            );
            var tokenstring = new JwtSecurityTokenHandler().WriteToken(token);

            var refreshtoken = Guid.NewGuid().ToString();
            var exptoken = DateTime.UtcNow.AddHours(2);
            var tokendto = new TokenDTO { Token = tokenstring, RefrashToken = refreshtoken, Expiration = exp, ExpirationRefreshToken = exptoken };
            return tokendto;
        }
    }

}