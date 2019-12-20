using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using GammaService.Common;

namespace GammaService
{
    public static class Db
    {
        public static Guid? CreateNewPallet(int placeId, string modbusName)
        {
            try
            {
                using (var gammaBase = new GammaEntities())
                {
                    var userInfo = gammaBase.CurrentPlaceUsers.FirstOrDefault(pu => pu.PlaceID == placeId);
                    if (userInfo == null) return null;
                    var productionTask =
                        gammaBase.ActiveProductionTasks.FirstOrDefault(pt => pt.PlaceID == placeId)?.ProductionTasks;
                    if (productionTask == null) return null;
                    var currentDateTime = CurrentDateTime(gammaBase);
                    var baseQuantity =
                        (int) (gammaBase.C1CCharacteristics.Where(
                            c => c.C1CCharacteristicID == productionTask.C1CCharacteristicID)
                            .Select(c => c.C1CMeasureUnitsPallet.Coefficient).First() ?? 0);
                    var productId = SqlGuidUtil.NewSequentialid();
                    var docId = SqlGuidUtil.NewSequentialid();
                    var doc = new Docs
                    {
                        DocID = docId,
                        Date = currentDateTime,
                        DocTypeID = (int) DocType.DocProduction,
                        IsConfirmed = true,
                        PlaceID = userInfo.PlaceID,
                        ShiftID = userInfo.ShiftID,
                        UserID = userInfo.UserID,
                        PrintName = userInfo.PrintName,
                        DocProduction = new DocProduction
                        {
                            DocID = docId,
                            InPlaceID = placeId,
                            ProductionTaskID = productionTask.ProductionTaskID,
                            HasWarnings = false,
                            DocProductionProducts = new List<DocProductionProducts>
                            {
                                new DocProductionProducts
                                {
                                    DocID = docId,
                                    ProductID = productId,
                                    C1CNomenclatureID = (Guid) productionTask.C1CNomenclatureID,
                                    C1CCharacteristicID = (Guid) productionTask.C1CCharacteristicID,
                                    Quantity = baseQuantity,
                                    Products = new Products
                                    {
                                        ProductID = productId,
                                        ProductKindID = (int) ProductKind.ProductPallet,
                                        ProductPallets = new ProductPallets
                                        {
                                            ProductID = productId,
                                            ProductItems = new List<ProductItems>
                                            {
                                                new ProductItems
                                                {
                                                    ProductItemID = SqlGuidUtil.NewSequentialid(),
                                                    ProductID = productId,
                                                    C1CNomenclatureID = (Guid) productionTask.C1CNomenclatureID,
                                                    C1CCharacteristicID = (Guid) productionTask.C1CCharacteristicID,
                                                    Quantity = baseQuantity
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    };
                    gammaBase.Docs.Add(doc);
                    var sourceSpools = gammaBase.GetActiveSourceSpools(placeId).ToList();
                    foreach (var spoolId in sourceSpools.Where(s => s != null))
                    {
                        var docWithdrawalProduct =
                            gammaBase.DocWithdrawalProducts.Include(d => d.DocWithdrawal)
                                .Include(d => d.DocWithdrawal.Docs)
                                .Include(d => d.DocWithdrawal.DocProduction)
                                .FirstOrDefault(d => d.ProductID == spoolId
                                                     && d.Quantity == null &&
                                                     (d.CompleteWithdrawal == null ||
                                                      d.CompleteWithdrawal == false));
                        if (docWithdrawalProduct == null)
                        {
                            var docWithdrawalId = SqlGuidUtil.NewSequentialid();
                            docWithdrawalProduct = new DocWithdrawalProducts
                            {
                                DocID = docWithdrawalId,
                                ProductID = (Guid) spoolId,
                                DocWithdrawal = new DocWithdrawal
                                {
                                    DocID = docWithdrawalId,
                                    OutPlaceID = placeId,
                                    Docs = new Docs
                                    {
                                        DocID = docWithdrawalId,
                                        Date = currentDateTime,
                                        DocTypeID = (int) DocType.DocWithdrawal,
                                        PlaceID = userInfo.PlaceID,
                                        UserID = userInfo.UserID,
                                        ShiftID = userInfo.ShiftID,
                                        PrintName = userInfo.PrintName,
                                        IsConfirmed = false
                                    }
                                }
                            };
                            gammaBase.DocWithdrawalProducts.Add(docWithdrawalProduct);
                        }
                        if (docWithdrawalProduct.DocWithdrawal.DocProduction == null)
                            docWithdrawalProduct.DocWithdrawal.DocProduction = new List<DocProduction>();
                        docWithdrawalProduct.DocWithdrawal.DocProduction.Add(doc.DocProduction);
                    }
                    try
                    {
                        gammaBase.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        Common.Console.WriteLine(modbusName, $"{DateTime.Now}: Ошибка Создания паллеты в базе");
                        Common.Console.WriteLine(modbusName, ex.Message + ":" + ex.InnerException);
                        return null;
                    }
                    return docId;
                }
            }
            catch (Exception ex)
            {
                Common.Console.WriteLine(modbusName, $"{DateTime.Now}: Ошибка Создания паллеты в базе");
                Common.Console.WriteLine(modbusName, ex.Message + ":" + ex.InnerException);
                return null;
            }
        }

        public static DateTime CurrentDateTime(GammaEntities gammaBase)
        {
            return ((IObjectContextAdapter)gammaBase).ObjectContext.CreateQuery<DateTime>("CurrentDateTime()").AsEnumerable().First();
        } 
    }
}
