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

namespace TalkToApi.V1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userInManager;
        private readonly ITokenRepository _tokenrepository;
        public UsuarioController(IUsuarioRepository usuarioRepository,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userInManager,
            ITokenRepository tokenrepository)
        {
            _usuarioRepository = usuarioRepository;
            _signInManager = signInManager;
            _userInManager = userInManager;
            _tokenrepository = tokenrepository;
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

        [HttpPost("")]
        public ActionResult Cadastrar([FromBody]UsuarioDTO usuarioDTO)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser usuario = new ApplicationUser();
                usuario.FullName = usuarioDTO.Nome;
                usuario.UserName = usuarioDTO.Email;
                usuario.Email = usuarioDTO.Email;
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
                    return Ok(usuario);
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