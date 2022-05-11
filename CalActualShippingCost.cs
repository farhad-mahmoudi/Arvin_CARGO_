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
    public class CalActualShippingCost : CodeActivity
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
                new ColumnSet("new_totalshippingcost", "new_totalactualweight"));
            //هزینه حمل واقعی محموله
            decimal totalShippingCost = 0;
            if (cargoEntity.Contains("new_totalshippingcost") == true)
            {
                totalShippingCost = (decimal)cargoEntity["new_totalshippingcost"];
            }

            //مجموع وزن واقعی
            decimal totalActualWeight = 0;
            if (cargoEntity.Contains("new_totalactualweight") == true)
            {
                totalActualWeight = (decimal)cargoEntity["new_totalactualweight"];
            }
            //ردیف های  محموله
            QueryExpression qe = new QueryExpression("new_cargorow");
            qe.Criteria.AddCondition("new_relatedcargo", ConditionOperator.Equal, cargoRef.Id);
            qe.ColumnSet = new ColumnSet("new_totalactualweight");
            EntityCollection cargoRows = service.RetrieveMultiple(qe);

            foreach (var row in cargoRows.Entities)
            {
                //وزن واقعی کل
                decimal totalActualWeightRow = 0;
                if (row.Contains("new_totalactualweight") == true)
                {
                    totalActualWeightRow = (decimal)row["new_totalactualweight"];
                }
                //هزینه حمل واقعی
                decimal actualShippingCost = 0;
                if (totalActualWeight != 0 && totalActualWeightRow != 0 && totalShippingCost != 0)
                {
                    actualShippingCost = (totalShippingCost * totalActualWeightRow) / totalActualWeight;
                }

                Entity cargoRow = new Entity("new_cargorow", row.Id);
                cargoRow["new_actualshippingcost"] = actualShippingCost;

                service.Update(cargoRow);
            }


        }
    }
}
