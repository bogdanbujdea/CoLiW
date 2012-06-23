#region License

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="{Faculty of Computer Science A. I. Cuza}">
//
// CoLiW is an eperiment for using web applications in the command line
// Copyright (C) 2012 Faculty of Computer Science A. I. Cuza
//
// This program is free software: you can redistribute it and/or modify
// it under the +terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see http://www.gnu.org/licenses/. 
// </copyright>
// <summary>
// Email: coliw@gmail.com
// </summary>
// -------------------------------------------------------------------------------------------------------------------

#endregion
using System.IO;

namespace CoLiW
{
    public class PhotoDetails
    {
        public byte[] ImageStream { get; set; }

        public string AlbumId { get; set; }

        public string FacebookName { get; set; }

        public string AlbumName { get; set; }

        public string AlbumDescription { get; set; }

        public string PhotoDescription { get; set; }

    }
}