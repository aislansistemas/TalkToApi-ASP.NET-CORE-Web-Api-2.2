using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TalkToApi.DataBase;
using TalkToApi.V1.Models;
using TalkToApi.V1.Repositories.Contracts;

namespace TalkToApi.V1.Repositories
{
    public class TokenRepository : ITokenRepository
    {
        private readonly TalkToContext _banco;
        public TokenRepository(TalkToContext contexnto)
        {
            _banco = contexnto; 
        }
        public void Atualizar(Token token)
        {
            _banco.Token.Update(token);
            _banco.SaveChanges();
        }

        public void Cadastrar(Token token)
        {
            _banco.Token.Add(token);
            _banco.SaveChanges();
        }

        public Token Obter(string refreshtoken)
        {
            return _banco.Token.FirstOrDefault(a => a.RefreshToken == refreshtoken && a.Utilizado==false);
        }
    }
}
