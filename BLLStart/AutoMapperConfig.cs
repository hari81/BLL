using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AutoMapper;

namespace BLL.BLLStart
{
    public static class AutoMapperConfig
    {
        public static void Configure()
        {
            Mapper.Initialize(
                conf=> {
                    conf.CreateMap<DAL.GENERAL_EQ_UNIT, Core.Domain.GeneralComponent>()
                    .ForMember(dest => dest.Id,
                               opts => opts
                               .MapFrom(src => src.equnit_auto)
                               )
                    .ForMember(dest => dest.CompartId,
                               opts => opts
                               .MapFrom(src => src.compartid_auto)
                               )
                    .ForMember(dest => dest.ComponentLifeAtInstall,
                               opts => opts
                               .MapFrom(src => src.smu_at_install)
                               )
                    .ForMember(dest => dest.ComponentStatus,
                               opts => opts
                               .MapFrom(src => src.comp_status)
                               )
                    .ForMember(dest => dest.CreatedByUserName,
                               opts => opts
                               .MapFrom(src => src.created_user)
                               )
                    .ForMember(dest => dest.EquipmentId,
                               opts => opts
                               .MapFrom(src => src.equipmentid_auto)
                               )
                    .ForMember(dest => dest.EquipmentLifeAtInstall,
                               opts => opts
                               .MapFrom(src => src.eq_ltd_at_install)
                               )
                    .ForMember(dest => dest.EquipmentSMUatInstall,
                               opts => opts
                               .MapFrom(src => src.eq_smu_at_install)
                               )
                    .ForMember(dest => dest.InstallDate,
                               opts => opts
                               .MapFrom(src => src.date_installed)
                               )
                    .ForMember(dest => dest.Position,
                               opts => opts
                               .MapFrom(src => src.pos)
                               )
                    .ForMember(dest => dest.Side,
                               opts => opts
                               .MapFrom(src => src.side)
                               )
                    .ForMember(dest => dest.UCSystemId,
                               opts => opts
                               .MapFrom(src => src.module_ucsub_auto)
                               )
                    .ForMember(dest => dest.UCSystemLifeAtInstall,
                               opts => opts
                               .MapFrom(src => src.system_LTD_at_install)
                               )
                    .ForMember(dest => dest.Worn,
                               opts => opts
                               .MapFrom(src => src.track_0_worn)
                               )
                    .ForMember(dest => dest.Worn100,
                               opts => opts
                               .MapFrom(src => src.track_100_worn)
                               )
                    .ForMember(dest => dest.Worn120,
                               opts => opts
                               .MapFrom(src => src.track_120_worn)
                               )
                    .ForMember(dest => dest.BudgetedLife,
                               opts => opts
                               .MapFrom(src => src.track_budget_life)
                               )
                    .ForMember(dest => dest.Cost,
                               opts => opts
                               .MapFrom(src => src.cost)
                               );
                }); //Component configuration

            Mapper.Initialize(
                conf => {
                    conf.CreateMap<DAL.USER_TABLE, Core.Domain.User>()
                    .ForMember(dest => dest.Id,
                               opts => opts
                               .MapFrom(src => src.user_auto)
                               )
                    .ForMember(dest => dest.userStrId,
                               opts => opts
                               .MapFrom(src => src.userid)
                               )
                    .ForMember(dest => dest.userName,
                               opts => opts
                               .MapFrom(src => src.username)
                               );
                });
        }
    }
}