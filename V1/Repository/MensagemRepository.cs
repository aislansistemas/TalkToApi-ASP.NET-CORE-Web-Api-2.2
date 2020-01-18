﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TalkToApi.DataBase;
using TalkToApi.V1.Models;
using TalkToApi.V1.Repository.Contracts;

namespace TalkToApi.V1.Repository
{
    public class MensagemRepository : IMensagemRepository
    {
        private readonly TalkToContext _banco;
        public MensagemRepository(TalkToContext banco)
        {
            _banco = banco;
        }

        public void Atualizar(Mensagem mensagem)
        {
            _banco.Mensagem.Update(mensagem);
            _banco.SaveChanges();
        }

        public void Cadastrar(Mensagem mensagem)
        {
            _banco.Mensagem.Add(mensagem);
            _banco.SaveChanges();
        }

        public Mensagem Obter(int id)
        {
            return _banco.Mensagem.Find(id);
        }

        public List<Mensagem> ObterMensagens(string usuarioUmId, string usuarioDoisId)
        {
            return _banco.Mensagem.Where(x => (x.DeId == usuarioUmId || x.DeId == usuarioDoisId) && (x.ParaId == usuarioUmId || x.ParaId == usuarioDoisId)).ToList();
        }
    }
}