using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Domain.Entities;

namespace ClinicManagementSystem.Application.Mappings;

public class AutoMapperProfiles : Profile
{
    public AutoMapperProfiles()
    {
        // ─── Patient ───
        CreateMap<CreatePatientDto, Patient>();
        CreateMap<Patient, PatientResponseDto>();

        // ─── Appointment ───
        CreateMap<CreateAppointmentDto, Appointment>();
        CreateMap<Appointment, AppointmentResponseDto>()
            .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => $"{src.Patient.FirstName} {src.Patient.LastName}"))
            .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => $"{src.Doctor.FirstName} {src.Doctor.LastName}"))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        // ─── VitalSign ───
        CreateMap<RecordVitalsDto, VitalSign>();
        CreateMap<VitalSign, VitalSignResponseDto>()
            .ForMember(dest => dest.RecordedByNurse, opt => opt.MapFrom(src => $"{src.Nurse.FirstName} {src.Nurse.LastName}"));

        // ─── Consultation ───
        CreateMap<CreateConsultationDto, Consultation>();
        CreateMap<Consultation, ConsultationResponseDto>()
            .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => $"{src.Doctor.FirstName} {src.Doctor.LastName}"))
            .ForMember(dest => dest.Prescription, opt => opt.MapFrom(src => src.Prescription));

        // ─── Prescription ───
        CreateMap<Prescription, PrescriptionResponseDto>();
        CreateMap<PrescriptionItem, PrescriptionItemResponseDto>()
            .ForMember(dest => dest.MedicineName, opt => opt.MapFrom(src => src.Medicine.Name))
            .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.TotalPrice));

        // ─── Medicine ───
        CreateMap<CreateMedicineDto, Medicine>();
        CreateMap<Medicine, MedicineResponseDto>();

        // ─── Invoice ───
        CreateMap<Invoice, InvoiceResponseDto>()
            .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => $"{src.Patient.FirstName} {src.Patient.LastName}"))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.LineItems, opt => opt.MapFrom(src => src.LineItems))
            .ForMember(dest => dest.BalanceDue, opt => opt.MapFrom(src => src.BalanceDue));

        CreateMap<InvoiceItem, InvoiceItemResponseDto>()
            .ForMember(dest => dest.LineTotal, opt => opt.MapFrom(src => src.LineTotal));

        CreateMap<CreateInvoiceItemDto, InvoiceItem>();

        // ─── Payment ───
        CreateMap<Payment, PaymentResponseDto>();
    }
}