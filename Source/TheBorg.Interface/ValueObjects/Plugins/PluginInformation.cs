// The MIT License(MIT)
// 
// Copyright(c) 2016 Rasmus Mikkelsen
// https://github.com/rasmus/TheBorg
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

using System;

namespace TheBorg.Interface.ValueObjects.Plugins
{
    public class PluginInformation : ValueObject
    {
        public static PluginInformation With(
            PluginId id,
            PluginTitle title,
            PluginVersion version,
            PluginDescription description,
            Uri uri)
        {
            return new PluginInformation(
                id,
                title,
                version,
                description,
                uri);
        }

        public PluginInformation(
            PluginId id,
            PluginTitle title,
            PluginVersion version,
            PluginDescription description,
            Uri uri)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (title == null) throw new ArgumentNullException(nameof(title));
            if (version == null) throw new ArgumentNullException(nameof(version));
            if (description == null) throw new ArgumentNullException(nameof(description));
            if (uri == null) throw new ArgumentNullException(nameof(uri));

            Id = id;
            Title = title;
            Version = version;
            Description = description;
            Uri = uri;
        }

        public PluginId Id { get; }
        public PluginTitle Title { get; }
        public PluginVersion Version { get; }
        public PluginDescription Description { get; }
        public Uri Uri { get; }
    }
}