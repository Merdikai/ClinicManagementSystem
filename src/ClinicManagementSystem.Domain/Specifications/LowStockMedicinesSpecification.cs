using ClinicManagementSystem.Domain.Entities;

namespace ClinicManagementSystem.Domain.Specifications;

public class LowStockMedicinesSpecification : BaseSpecification<Medicine>
{
    public LowStockMedicinesSpecification()
        : base(m => m.IsActive && m.StockQuantity <= m.ReorderLevel)
    {
        ApplyOrderBy(m => m.StockQuantity);
    }
}
