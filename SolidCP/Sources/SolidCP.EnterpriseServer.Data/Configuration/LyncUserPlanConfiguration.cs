﻿using System;
using System.Collections.Generic;
using SolidCP.EnterpriseServer.Data.Configuration;
using SolidCP.EnterpriseServer.Data.Entities;
using System.ComponentModel.DataAnnotations.Schema;
#if NetCore
using Microsoft.EntityFrameworkCore;
#endif
#if NetFX
using System.Data.Entity;
#endif

namespace SolidCP.EnterpriseServer.Data.Configuration;

public partial class LyncUserPlanConfiguration: EntityTypeConfiguration<LyncUserPlan>
{
    public override void Configure() {

#if NetCore
        HasOne(d => d.Item).WithMany(p => p.LyncUserPlans).HasConstraintName("FK_LyncUserPlans_ExchangeOrganizations");
#else
        HasRequired(d => d.Item).WithMany(p => p.LyncUserPlans);
#endif
    }
}