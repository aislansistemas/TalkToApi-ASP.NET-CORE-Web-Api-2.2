using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TalkToApi.V1.Models;
using TalkToApi.V1.Repositories.Contracts;

namespace TalkToApi.V1.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly UserManager<ApplicationUser> _usermaneger;

        public UsuarioRepository(UserManager<ApplicationUser> usermaneger)
        {
            _usermaneger = usermaneger;  
        }
        public void Cadastrar(ApplicationUser usuario,string senha)
        {
            var result= _usermaneger.CreateAsync(usuario, senha).Result;
            if (!result.Succeeded)
            {
                StringBuilder sb = new StringBuilder();
                foreach(var erro in result.Errors)
                {
                    sb.Append(erro.Description);
                }
                throw new Exception($"Usuario não cadastrado {sb.ToString()}");
            }

        }

        public ApplicationUser Obter(string email, string senha)
        {
            var usuario= _usermaneger.FindByNameAsync(email).Result;
            if (_usermaneger.CheckPasswordAsync(usuario, senha).Result) 
            {
                return usuario;
            }
            else
            {
                throw new Exception("Usuario não localizado");
            }
        }

        public ApplicationUser Obter(string id)
        {
            return _usermaneger.FindByIdAsync(id).Result;
        }
    }
}
