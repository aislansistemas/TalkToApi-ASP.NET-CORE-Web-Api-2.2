using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using TalkToApi.Helpers.Contants;
using TalkToApi.v1.Models.DTO;
using TalkToApi.V1.Models;
using TalkToApi.V1.Models.DTO;
using TalkToApi.V1.Repository.Contracts;

namespace TalkToApi.V1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors]
    public class MensagemController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IMensagemRepository _mensagemRepository;
        public MensagemController(IMensagemRepository mensagemRepository, IMapper mapper)
        {
            _mensagemRepository = mensagemRepository;
            _mapper = mapper;
        }
        [Authorize]
        [HttpGet("{usuarioUmId}/{usuarioDoisId}",Name ="MensagemObter")]
        public ActionResult Obter(string usuarioUmId,string usuarioDoisId,[FromHeader(Name ="Accept")]string mediaType)
        {
            if (usuarioUmId == usuarioDoisId)
            {
                return UnprocessableEntity();
            }
            var mensagens = _mensagemRepository.ObterMensagens(usuarioUmId, usuarioDoisId);
            if (mediaType == CustomMediaTypes.Hateoas)
            {
                
                var listaMSG = _mapper.Map<List<Mensagem>, List<MensagemDTO>>(mensagens);
                var lista = new ListaDTO<MensagemDTO>() { Lista = listaMSG };
                lista.Links.Add(new LinkDTO("self", Url.Link("MensagemObter", new { usuarioUmId = usuarioUmId, usuarioDoisId = usuarioDoisId }), "GET"));
                return Ok();
            }
            else
            {   

                return Ok(mensagens);
            }
            
        }
        [Authorize]
        [HttpPost("",Name = "MensagemCadastrar")]
        public ActionResult Cadastrar([FromBody]Mensagem mensagem, [FromHeader(Name = "Accept")]string mediaType)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _mensagemRepository.Cadastrar(mensagem);
                    if (mediaType == CustomMediaTypes.Hateoas)
                    {
                        var mensageDB = _mapper.Map<Mensagem, MensagemDTO>(mensagem);
                        mensageDB.Links.Add(new LinkDTO("self", Url.Link("MensagemCadastrar", null), "POST"));
                        mensageDB.Links.Add(new LinkDTO("atualizacaoParcial", Url.Link("MensagemAtualizacaoParcial", new { id = mensagem.Id }), "PATCH"));

                        return Ok(mensageDB);
                    }
                    else
                    {
                        return Ok(mensagem);
                    }
                }
                catch(Exception e)
                {
                    return UnprocessableEntity(e);
                }
                
            }
            else
            {
                return UnprocessableEntity(ModelState);
            }
        }
            [Authorize]
            [HttpPatch("{id}",Name = "MensagemAtualizacaoParcial")]
            public ActionResult AtualizacaoParcial(int id,[FromBody]JsonPatchDocument<Mensagem> jsonPatch, [FromHeader(Name = "Accept")]string mediaType)
            {
                if(jsonPatch == null){
                    return BadRequest();
                }
                var mensagem=_mensagemRepository.Obter(id);
                jsonPatch.ApplyTo(mensagem);
                mensagem.Atualizado=DateTime.UtcNow;
                _mensagemRepository.Atualizar(mensagem);

            if (mediaType == CustomMediaTypes.Hateoas)
            {
                var mensageDB = _mapper.Map<Mensagem, MensagemDTO>(mensagem);
                mensageDB.Links.Add(new LinkDTO("self", Url.Link("MensagemAtualizacaoParcial", new { id = mensagem.Id }), "PATCH"));

                return Ok(mensageDB);
            }
            else
            {
                return Ok(mensagem);
            }

            }
        }
    }
