using ClinicManagementSystem.Application.DTOs;
using MediatR;

namespace ClinicManagementSystem.Application.Medicines.Queries;

public record GetMedicinesPagedQuery(int Page, int PageSize, string? Search) : IRequest<PagedResponse<MedicineResponseDto>>;
