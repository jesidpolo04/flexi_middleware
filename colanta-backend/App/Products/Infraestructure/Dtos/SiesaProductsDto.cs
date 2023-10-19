namespace colanta_backend.App.Products.Infraestructure
{
    using System;
    using Products.Domain;
    using Brands.Domain;
    using Categories.Domain;
    public class SiesaProductsDto
    {
        public SiesaProductDto[] productos { get; set; }
    }

    public class SiesaProductDto
    {
        public int tipo { get; set; }
        public string id { get; set; }
        public string nombre { get; set; }
        public string referencia { get; set; }
        public string? ean { get; set; }
        public string? descripcion { get; set; }
        public string id_marca { get; set; }
        public string id_linea { get; set; }
        public string tipo_producto { get; set; }
        public string unidad_medida { get; set; }
        public decimal iva_aplica { get; set; }
        public string negocio { get; set; }
        public DimensionesDto? dimensiones { get; set; }
        public VariacionDto[] variaciones { get; set; }

        public Product getProductFromDto()
        {
            if (this.tipo == 1)
            {
                Product product = new Product
                {
                    type = this.tipo,
                    siesa_id = this.id,
                    concat_siesa_id = this.negocio + "_" + this.id,
                    name = this.nombre,
                    ref_id = this.referencia,
                    description = this.descripcion,
                    business = this.negocio
                };
                product.setBrand(new Brand(null, id_siesa: this.id_marca));
                product.setCategory(new Category("", siesa_id: this.id_linea));

                Sku sku = new Sku
                {    
                    siesa_id = this.id,
                    ean = this.ean,
                    concat_siesa_id = this.negocio + "_" + this.id,
                    name = this.nombre,
                    ref_id = this.id,
                    description = this.descripcion,
                    measurement_unit = this.unidad_medida,
                    packaged_height = this.dimensiones.alto_producto,
                    packaged_width = this.dimensiones.ancho_producto,
                    packaged_length = this.dimensiones.largo_producto,
                    packaged_weight_kg = this.dimensiones.peso_bruto
                };
                sku.setProduct(product);
                product.addSku(sku);

                return product;
            }
            else
            {
                Product product = new Product
                {
                    type = this.tipo,
                    siesa_id = this.id,
                    concat_siesa_id = this.negocio + "_" + this.id,
                    name = this.nombre,
                    ref_id = this.referencia,
                    description = this.descripcion,
                    business = this.negocio
                };
                product.setBrand(new Brand(null, id_siesa: this.id_marca));
                product.setCategory(new Category("", siesa_id: this.id_linea));


                foreach (VariacionDto variacionDto in this.variaciones)
                {
                    Sku sku = new Sku
                    {
                        siesa_id = variacionDto.id,
                        ean = variacionDto.ean,
                        concat_siesa_id = this.negocio + "_" + variacionDto.id,
                        name = variacionDto.nombre,
                        description = this.descripcion,
                        ref_id = variacionDto.id,
                        measurement_unit = this.unidad_medida,
                        packaged_height = variacionDto.dimensiones.alto_producto,
                        packaged_width = variacionDto.dimensiones.ancho_producto,
                        packaged_length = variacionDto.dimensiones.largo_producto,
                        packaged_weight_kg = variacionDto.dimensiones.peso_bruto
                    };
                    sku.setProduct(product);

                    product.addSku(sku);
                }
                return product;
            }
        }
    }

    public class DimensionesDto
    {
        public double alto_producto { get; set; }
        public double ancho_producto { get; set; }
        public double largo_producto { get; set; }
        public double peso_neto { get; set; }
        public double peso_bruto { get; set; }
    }

    public class VariacionDto
    {
        public string id { get; set; }
        public string nombre { get; set; }
        public string ean { get; set; }
        public string referencia { get; set; }
        public DimensionesDto dimensiones { get; set; }

    }
}
