﻿namespace colanta_backend.App.Users.Domain
{
    using System.Threading.Tasks;
    using System.Text.RegularExpressions;
    using System;
    using Shared.Domain;
    public class RegisterUserService
    {
        private UsersSiesaRepository siesaRepository;
        private UsersVtexRepository vtexRepository;
        private UsersRepository localRepository;
        private WrongAddressesRepository wrongAddressesRepository;

        public RegisterUserService(UsersSiesaRepository siesaRepository, UsersVtexRepository vtexRepository, UsersRepository localRepository, WrongAddressesRepository wrongAddressesRepository)
        {
            this.siesaRepository = siesaRepository;
            this.vtexRepository = vtexRepository;
            this.localRepository = localRepository;
            this.wrongAddressesRepository = wrongAddressesRepository;
        }

        public async Task registerUser
            (
                string vtexId,
                string country,
                string departament,
                string city,
                string business
            )
        {
            var addressCorrector = new AddressCorrector(this.wrongAddressesRepository);
            VtexUser vtexUser = this.vtexRepository.getByVtexId(vtexId).Result;
            if(vtexUser == null) return;
            User user = VtexUserMapper.getUserFromVtexUser(vtexUser);
            user.country_code = "Colombia";
            user.department_code = addressCorrector.correctStateIfIsWrong(country, departament, city);
            user.city_code = addressCorrector.correctCityIfIsWrong(country, departament, city);
            user.born_date = DateTime.Now.ToString("yyyyMMdd");
            user.business = business;
            user = await this.siesaRepository.saveUser(user);
            if(this.localRepository.getUserByEmail(vtexUser.email).Result == null)
            {
                await this.localRepository.saveUser(user);
            }
            this.vtexRepository.setCustomerClass(vtexUser.id, user.client_type).Wait();
        }   
    }
}
