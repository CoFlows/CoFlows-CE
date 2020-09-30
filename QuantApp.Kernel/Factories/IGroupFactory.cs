/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;

namespace QuantApp.Kernel.Factories
{
    public interface IGroupFactory
    {
        Group CreateGroup(User user, string name);
        Group CreateGroup(User user, string name, string id);

        Group FindGroup(User user, string id);
        Group FindGroupByName(User user, string name);

        T GetProperty<T>(Group group, string name);
        void SetProperty(Group group, string name, object value);

        void Remove(Group group);

        List<IPermissible> List(User user, Group group, Type type, bool aggregated);
        List<IPermissible> List(User user, Group group, Type type, AccessType accessType, bool aggregated);

        AccessType Permission(User user, Group group, IPermissible permissible);
        DateTime Expiry(User user, Group group, IPermissible permissible);
        bool Exists(Group group, IPermissible permissible);

        void Add(Group group, IPermissible permissible, Type type, AccessType accessType, DateTime? expiry);
        void Remove(Group group, IPermissible permissible);

        List<Group> SubGroups(User user, Group group, bool aggregated);
        void Add(Group group, Group subgroup);
        void Remove(Group group, Group subgroup);

        List<Group> Groups(User user, IPermissible permissible, AccessType type);
        List<Group> Groups(User user, IPermissible permissible);

        List<Group> Groups(User user);
        List<Group> MasterGroups(User user);
    }
}
