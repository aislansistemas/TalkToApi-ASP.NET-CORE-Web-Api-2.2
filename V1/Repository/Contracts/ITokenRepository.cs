using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TalkToApi.V1.Models;

namespace TalkToApi.V1.Repositories.Contracts
{
    public interface ITokenRepository
    {
        void Cadastrar(Token token);
        Token Obter(string refreshtoken);
        void Atualizar(Token token);
    }
}
