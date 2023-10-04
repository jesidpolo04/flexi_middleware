﻿namespace colanta_backend.App.Brands.Infraestructure
{
    using App.Brands.Domain;
    using System;
    public class SiesaBrandMapper
    {
        public SiesaBrandDTO entityToDto(Brand brand)
        {
            SiesaBrandDTO siesaBrandDto = new SiesaBrandDTO();
            siesaBrandDto.id = brand.id_siesa;
            siesaBrandDto.nombre = brand.name;
            siesaBrandDto.negocio = brand.business;
            return siesaBrandDto;
        }

        public Brand DtoToEntity(SiesaBrandDTO siesaBrand)
        {
            return new Brand(
                            name: siesaBrand.nombre,
                            id_siesa: siesaBrand.id,
                            business: siesaBrand.negocio
                       );
        }
    }
}
