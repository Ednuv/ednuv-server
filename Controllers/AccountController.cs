using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.Entities;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using API.Dto;
using API.Interface;

namespace API.Controllers
{
    public class AccountController: BaseApiController
    {
        private readonly DataContext context;
        public ITokenService TokenService { get; }
        public AccountController(DataContext context, ITokenService tokenService)
        {
            this.TokenService = tokenService;
           this.context = context;
        }
        
        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            if(await UserExits(registerDto.username)) return BadRequest("Username is taken");
            using var hmac = new HMACSHA512();
            var user = new AppUser
            {
                UserName = registerDto.username.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.password)),
                PasswordSalt = hmac.Key,
                FirstName = registerDto.firstName,
                LastName = registerDto.lastName,
                Phone = registerDto.phone
            };
            this.context.Users.Add(user);
            await this.context.SaveChangesAsync();

            return new UserDto
            {
                Username = user.UserName,
                Token = this.TokenService.CreateToken(user)
            };
        }
        public async Task<bool> UserExits(string username){
            return await this.context.Users.AnyAsync(x=>x.UserName == username.ToLower());
        }
        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await this.context.Users.SingleOrDefaultAsync(x=> x.UserName == loginDto.username);
            if(user ==null) return Unauthorized("Invalid username");

            using var hmac = new  HMACSHA512(user.PasswordSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.password));

            for(int i=0;i<computedHash.Length;i++) {
               if(computedHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid Password");
            }
             return new UserDto
            {
                Username = user.UserName,
                Token = this.TokenService.CreateToken(user)
            };
        }
    }
}