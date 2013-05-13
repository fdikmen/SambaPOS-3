﻿using System.ComponentModel.Composition;
using System.Linq;
using Samba.Domain.Models.Settings;
using Samba.Domain.Models.Tickets;

namespace Samba.Services.Implementations.PrinterModule.ValueChangers
{
    [Export]
    public class OrderValueChanger : AbstractValueChanger<Order>
    {
        private readonly OrderTagValueChanger _orderTagValueChanger;
        private readonly OrderStateValueChanger _orderStateValueChanger;

        [ImportingConstructor]
        public OrderValueChanger(OrderTagValueChanger orderTagValueChanger, OrderStateValueChanger orderStateValueChanger)
        {
            _orderTagValueChanger = orderTagValueChanger;
            _orderStateValueChanger = orderStateValueChanger;
        }

        public override string GetTargetTag()
        {
            return "ORDERS";
        }

        protected override bool FilterMatch(Order model, string key)
        {
            if (key.Contains("="))
            {
                var parts = key.Split('=');
                if (parts.Count() == 2)
                {
                    return model.IsInState(parts[0], parts[1]);
                }
            }
            return model.IsInState("*", key);
        }

        protected override string ReplaceTemplateValues(string templatePart, Order model, PrinterTemplate template)
        {
            var result = _orderStateValueChanger.Replace(template, templatePart, model.GetOrderStateValues());
            return _orderTagValueChanger.Replace(template, result, model.GetOrderTagValues());
        }

        protected override decimal GetSumSelector(Order x)
        {
            return x.GetValue();
        }

        protected override GroupingKey GetGroupSelector(Order arg, string switchValue)
        {
            if (!string.IsNullOrEmpty(switchValue) && switchValue.Contains(":"))
            {
                var parts = switchValue.Split(':');
                if (parts[0] == "ORDER TAG")
                {
                    var r = arg.GetOrderTagValue(parts[1]);
                    return new GroupingKey { Key = r.OrderKey, Name = r.TagValue };
                }
                if (parts[0] == "ORDER STATE")
                {
                    var r = arg.GetStateValue(parts[1]);
                    return new GroupingKey { Key = r.OrderKey, Name = r.StateValue };
                }
            }
            return base.GetGroupSelector(arg, switchValue);
        }

        protected override void ProcessItem(Order obj, string switchValue)
        {
            if (!string.IsNullOrEmpty(switchValue) && switchValue.Contains(":"))
            {
                var parts = switchValue.Split(':');
                if (parts[0] == "ORDER TAG" && obj.OrderTagExists(x => x.TagName == parts[1]))
                {
                    obj.GetOrderTagValues(x => x.TagName == parts[1]).ToList().ForEach(obj.UntagOrder);
                }
            }
        }
    }
}