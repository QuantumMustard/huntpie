﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HunterPie.Core;

namespace HunterPie.GUI.Widgets.DPSMeter {
    /// <summary>
    /// Interaction logic for DPSMeter.xaml
    /// </summary>
    public partial class Meter : Widget {

        private bool _IsActive;

        List<Parts.PartyMember> Players = new List<Parts.PartyMember>();
        Game GameContext;
        Party Context;
        public bool IsActive {
            get { return _IsActive; }
            set {
                if (value != _IsActive) {
                    _IsActive = value;
                    this.Visibility = (_IsActive && UserSettings.PlayerConfig.Overlay.DPSMeter.Enabled) ? Visibility.Visible : Visibility.Hidden;
                }
            }
        }

        public Meter(Game ctx) {
            InitializeComponent();
            SetWindowFlags(this);
            SetContext(ctx);
            ApplySettings();
        }

        public void SetContext(Game ctx) {
            Context = ctx.Player.PlayerParty;
            GameContext = ctx;
            HookEvents();
            this.Party.ItemsSource = Players;
        }

        private void HookEvents() {
            Context.OnTotalDamageChange += OnTotalDamageChange;
            GameContext.Player.OnPeaceZoneEnter += OnPeaceZoneEnter;
            GameContext.Player.OnPeaceZoneLeave += OnPeaceZoneLeave;
        }

        private void OnPeaceZoneLeave(object source, EventArgs args) {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => {
                CreatePlayerComponents();
                if (UserSettings.PlayerConfig.Overlay.DPSMeter.Enabled) Visibility = Context.TotalDamage > 0 ? Visibility.Visible : Visibility.Collapsed;
                else { Visibility = Visibility.Collapsed; }
            }));
        }

        private void OnPeaceZoneEnter(object source, EventArgs args) {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => {
                DestroyPlayerComponents();
                Visibility = Visibility.Collapsed;
            }));
        }

        public void UnhookEvents() {
            GameContext.Player.OnPeaceZoneEnter -= OnPeaceZoneEnter;
            GameContext.Player.OnPeaceZoneLeave -= OnPeaceZoneLeave;
            Context.OnTotalDamageChange -= OnTotalDamageChange;
            GameContext = null;
            Context = null;
        }

        private void OnTotalDamageChange(object source, EventArgs args) {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => {
                if (Context.TotalDamage > 0) this.IsActive = true;
                SortPlayersByDamage();
                if (Context.Epoch.TotalSeconds > 0 && UserSettings.PlayerConfig.Overlay.DPSMeter.ShowDPSWheneverPossible) {
                    TypeIcon.Visibility = Visibility.Visible;
                    Timer.Content = string.Format("{0:hh\\:mm\\:ss}", Context.Epoch);
                } else {
                    TypeIcon.Visibility = Visibility.Hidden;
                    Timer.Content = "Total Damage";
                }
            }));
        }

        private void CreatePlayerComponents() {
            for (int i = 0; i < Context.MaxSize; i++) {
                Parts.PartyMember pMember = new Parts.PartyMember(UserSettings.PlayerConfig.Overlay.DPSMeter.PartyMembers[i].Color);
                pMember.SetContext(Context[i], Context);
                Players.Add(pMember);
            }
            Party.Items.Refresh();
            if (Context.TotalDamage > 0) IsActive = true;
        }

        public void DestroyPlayerComponents() {
            foreach (Parts.PartyMember player in Players) {
                player.UnhookEvents();
            }
            Players.Clear();
            IsActive = false;
        }

        private void SortPlayersByDamage() {
            Players.Sort(delegate (Parts.PartyMember x, Parts.PartyMember y) {
                return x.CompareTo(y);
            });
            Party.Items.Refresh();
        }

        public void UpdatePlayersColor() {
            if (Players.Count <= 0) return;
            for (int i = 0; i < Context.MaxSize; i++) {
                Players[i].ChangeColor(UserSettings.PlayerConfig.Overlay.DPSMeter.PartyMembers[i].Color);
            }
            Party.Items.Refresh();
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e) {
            this.UnhookEvents();
        }

        public override void ApplySettings() {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => {
                this.Top = UserSettings.PlayerConfig.Overlay.DPSMeter.Position[1];
                this.Left = UserSettings.PlayerConfig.Overlay.DPSMeter.Position[0];
                this.Visibility = UserSettings.PlayerConfig.Overlay.DPSMeter.Enabled ? Visibility.Visible : Visibility.Hidden;
                ScaleWidget(0.8, 0.8);
            }));
        }
        
        public void ScaleWidget(double NewScaleX, double NewScaleY) {
            Width = BaseWidth * NewScaleX;
            Height = BaseHeight * NewScaleY;
            this.DamageContainer.LayoutTransform = new ScaleTransform(NewScaleX, NewScaleY);
        }

    }
}
