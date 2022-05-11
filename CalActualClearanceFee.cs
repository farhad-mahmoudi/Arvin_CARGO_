using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

namespace Cargo
{
   public class CalActualClearanceFee :CodeActivity
    {
        [RequiredArgument]
        [Input("محموله ")]
        [ReferenceTarget("new_cargo")]
        public InArgument<EntityReference> CargoIn { get; set; }
        protected override void Execute(CodeActivityContext executionContext)
        {
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            EntityReference cargoRef = CargoIn.Get(executionContext);
            Entity cargoEntity = service.Retrieve(cargoRef.LogicalName, cargoRef.Id,
                new ColumnSet("new_totalclearancefee", "new_totalsuppliedprice"));
            //هزینه ترخیص واقعی محموله
            decimal totalClearanceFee = 0;
            if (cargoEntity.Contains("new_totalclearancefee") == true)
            {
                totalClearanceFee = (decimal)cargoEntity["new_totalclearancefee"];
            }

            //مجموع قیمت تامین شده 
            decimal totalSupplierPrice = 0;
            if (cargoEntity.Contains("new_totalsuppliedprice") == true)
            {
                totalSupplierPrice = (decimal)cargoEntity["new_totalsuppliedprice"];
            }

            //ردیف های  محموله
            QueryExpression qe = new QueryExpression("new_cargorow");
            qe.Criteria.AddCondition("new_relatedcargo", ConditionOperator.Equal, cargoRef.Id);
            qe.ColumnSet = new ColumnSet("new_totalsuppliedprice");
            EntityCollection cargoRows = service.RetrieveMultiple(qe);

            foreach (var row in cargoRows.Entities)
            {
                //هزینه تامین شده کل
                decimal totalSupplierPriceRow = 0;
                if (row.Contains("new_totalsuppliedprice") == true)
                {
                    totalSupplierPriceRow = (decimal)row["new_totalsuppliedprice"];
                }
                //هزینه ترخیص واقعی 
                decimal actualClearenceFee = 0;
                if (totalSupplierPrice != 0 && totalSupplierPriceRow != 0 && totalClearanceFee != 0)
                {
                    actualClearenceFee = (totalClearanceFee * totalSupplierPriceRow) / totalSupplierPrice;
                }

                Entity cargoRow = new Entity("new_cargorow", row.Id);
                cargoRow["new_actualclearancefee"] = actualClearenceFee;

                service.Update(cargoRow);
            }


        }
    }
}
