﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorldDataExporter.Helper;

namespace RimWorldDataExporter.Model {
    class DataFaction : EData {
        public override string Category => "Faction";
        public override Type DefType => typeof(FactionDef);

        public bool isPlayer;
        public bool humanlikeFaction = true;
        public bool hidden;
        public bool autoFlee = true;
        public bool canSiege;
        public bool canStageAttacks;
        public bool canUseAvoidGrid = true;
        public float earliestRaidDays;
        public FloatRange allowedArrivalTemperatureRange = new FloatRange(-1000f, 1000f);
        public PawnKindDef basicMemberKind;
        public List<string> startingResearchTags;
        public List<string> recipePrerequisiteTags;
        public bool rescueesCanJoin;
        public float maxPawnOptionCostFactor = 1f;
        public int requiredCountAtGameStart;
        public int maxCountAtGameStart = 9999;
        public bool canMakeRandomly;
        public float baseSelectionWeight;
        public TechLevel techLevel;
        public string backstoryCategory;
        public List<string> hairTags = new List<string>();
        public float geneticVariance = 1f;
        public FloatRange startingGoodwill = FloatRange.Zero;
        public bool mustStartOneEnemy;
        public FloatRange naturalColonyGoodwill = FloatRange.Zero;
        public float goodwillDailyGain = 2f;
        public float goodwillDailyFall = 2f;
        public bool appreciative = true;
        public string homeIconPath;
        public string expandingIconTexture;
        public List<Color> colorSpectrum;


        [NoCrawl]
        public List<string> allMemberKinds;

        [NoCrawl]
        public List<string> apparelStuffs;

        [NoCrawl]
        public List<string> caravanTraderKinds;

        [NoCrawl]
        public List<string> visitorTraderKinds;

        [NoCrawl]
        public List<string> baseTraderKinds;

        public override void Crawl(Def baseDef) {
            base.Crawl(baseDef);

            FactionDef def = baseDef as FactionDef;

            if (def.pawnGroupMakers != null) {
                this.allMemberKinds = new List<string>();
                foreach (var pawnGroupMaker in def.pawnGroupMakers) {
                    if (pawnGroupMaker.options != null) {
                        foreach (var pawnGenOption in pawnGroupMaker.options) {
                            if (!this.allMemberKinds.Contains(pawnGenOption.kind.defName)) {
                                this.allMemberKinds.Add(pawnGenOption.kind.defName);
                            }
                        }
                    }
                    if (pawnGroupMaker.traders != null) {
                        foreach (var pawnGenOption in pawnGroupMaker.traders) {
                            if (!this.allMemberKinds.Contains(pawnGenOption.kind.defName)) {
                                this.allMemberKinds.Add(pawnGenOption.kind.defName);
                            }
                        }
                    }
                    if (pawnGroupMaker.guards != null) {
                        foreach (var pawnGenOption in pawnGroupMaker.guards) {
                            if (!this.allMemberKinds.Contains(pawnGenOption.kind.defName)) {
                                this.allMemberKinds.Add(pawnGenOption.kind.defName);
                            }
                        }
                    }
                }
            }

            if (def.apparelStuffFilter != null) {
                this.apparelStuffs = new List<string>();
                foreach (var thing in DefDatabase<ThingDef>.AllDefs.Where(d => def.apparelStuffFilter.Allows(d))) {
                    this.apparelStuffs.Add(thing.defName);
                }
            }

            if (def.caravanTraderKinds != null) {
                this.caravanTraderKinds = new List<string>();
                foreach (var trader in def.caravanTraderKinds) {
                    this.caravanTraderKinds.Add(trader.defName);
                }
            }
            if (def.visitorTraderKinds != null) {
                this.visitorTraderKinds = new List<string>();
                foreach (var trader in def.visitorTraderKinds) {
                    this.visitorTraderKinds.Add(trader.defName);
                }
            }
            if (def.baseTraderKinds != null) {
                this.baseTraderKinds = new List<string>();
                foreach (var trader in def.baseTraderKinds) {
                    this.baseTraderKinds.Add(trader.defName);
                }
            }
        }
    }

    class LangFaction : ELang {
        public override string Category => "Faction";
        public override Type DefType => typeof(FactionDef);

        public string fixedName;
        public string pawnsPlural;
        public string leaderTitle;
    }
}
