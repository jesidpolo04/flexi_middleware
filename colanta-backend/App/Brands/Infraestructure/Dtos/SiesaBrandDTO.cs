﻿namespace colanta_backend.App.Brands.Infraestructure
{
    public class SiesaBrandsDTO
    {
        public SiesaBrandDTO[] marcas { get; set; }
    }
    public class SiesaBrandDTO
    {
        public string id { get; set; } 
        public string nombre { get; set; }
        public string negocio { get; set; }
    }
}
