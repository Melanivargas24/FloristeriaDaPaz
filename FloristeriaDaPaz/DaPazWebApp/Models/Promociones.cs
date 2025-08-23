﻿using System.ComponentModel.DataAnnotations;

namespace DaPazWebApp.Models
{
    public class Promociones
    {
        public int? idPromocion { get; set; }

        [Required(ErrorMessage = "El nombre de la promoción es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres")]
        public string? nombrePromocion { get; set; }

        [Required(ErrorMessage = "El porcentaje de descuento es obligatorio")]
        [Range(0, 100, ErrorMessage = "El descuento debe estar entre 0 y 100")]
        public double? descuentoPorcentaje { get; set; }

        [Required(ErrorMessage = "La fecha de inicio es obligatoria")]
        [DataType(DataType.Date)]
        public DateTime? fechaInicio { get; set; }

        [Required(ErrorMessage = "La fecha de fin es obligatoria")]
        [DataType(DataType.Date)]
        public DateTime? fechaFin { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un producto")]
        public int? idProducto { get; set; }

        public string? nombreProducto { get; set; }

        [Required(ErrorMessage = "El estado es obligatorio")]
        public string? estado { get; set; } = "Activa";
    }
}