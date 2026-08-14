using ClinicManagementSystem.Application.DTOs;
using MediatR;

namespace ClinicManagementSystem.Application.Medicines.Queries;

public record GetMedicinesPagedQuery(int Page, int PageSize, string? Search, string? SortBy = null, bool Descending = false) : IRequest<PagedResponse<MedicineResponseDto>>;
