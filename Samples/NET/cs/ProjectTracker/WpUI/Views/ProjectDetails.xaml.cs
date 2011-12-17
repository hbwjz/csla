﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

namespace WpUI.Views
{
  public partial class ProjectDetails : PhoneApplicationPage
  {
    public ProjectDetails()
    {
      InitializeComponent();
    }

    private void EditButton_Click(object sender, EventArgs e)
    {
      var viewmodel = (ViewModels.ProjectDetail)this.DataContext;
      if (!viewmodel.CanEditObject)
        App.ViewModel.ShowError("Can't edit", "Authorization");
      else
        viewmodel.Edit();
    }

    private void DeleteButton_Click(object sender, EventArgs e)
    {
      var viewmodel = (ViewModels.ProjectDetail)this.DataContext;
      if (!viewmodel.CanEditObject)
        App.ViewModel.ShowError("Can't delete", "Authorization");
      else if (MessageBox.Show("Delete item?", "Project", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
        viewmodel.Delete();
    }

    private void CloseButton_Click(object sender, EventArgs e)
    {
      var viewmodel = (ViewModels.ProjectDetail)this.DataContext;
      viewmodel.Close();
    }
  }
}