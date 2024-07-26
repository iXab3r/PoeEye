using System.Linq;
using PoeShared.Tests.Helpers;

namespace PoeShared.Tests.Scaffolding;

public class NaturalStringComparerFixture : FixtureBase
{
    [Test]
    [TestCase("a,c,b", "a,b,c")]
    [TestCase("a1,b1,c1", "a1,b1,c1")]
    [TestCase("a,b,c", "a,b,c")]
    [TestCase("item2,item10,item1", "item1,item2,item10")]
    [TestCase("a1,a10,a2", "a1,a2,a10")]
    [TestCase("apple2,apple10,apple1", "apple1,apple2,apple10")]
    [TestCase("file1.txt,file10.txt,file2.txt", "file1.txt,file2.txt,file10.txt")]
    [TestCase("img12,img10,img2,img1", "img1,img2,img10,img12")]
    [TestCase("text100,text2,text1", "text1,text2,text100")]
    [TestCase("z9,z2,z10,z1", "z1,z2,z9,z10")]
    [TestCase("b3,a2,b10,a10", "a2,a10,b3,b10")]
    [TestCase("c,c2,c10,c1", "c,c1,c2,c10")]
    [TestCase("d1.2,d1.10,d1.1", "d1.1,d1.2,d1.10")]
    [TestCase("image1,image3,image10", "image1,image3,image10")]
    [TestCase("part2,part11,part1", "part1,part2,part11")]
    [TestCase("segment10,segment2,segment1", "segment1,segment2,segment10")]
    [TestCase("book9,book12,book1", "book1,book9,book12")]
    [TestCase("chapter2,chapter20,chapter1", "chapter1,chapter2,chapter20")]
    [TestCase("vol2,vol1,vol10", "vol1,vol2,vol10")]
    [TestCase("v1.1,v1.2,v1.10", "v1.1,v1.2,v1.10")]
    [TestCase("p1.9,p1.10,p1.2", "p1.2,p1.9,p1.10")]
    [TestCase("case100,case20,case1", "case1,case20,case100")]
    [TestCase("sec3,sec30,sec1", "sec1,sec3,sec30")]
    [TestCase("1item,10item,2item", "1item,2item,10item")]
    [TestCase("t10,t1,t2", "t1,t2,t10")]
    [TestCase("q1,q10,q2", "q1,q2,q10")]
    [TestCase("unit9,unit2,unit10", "unit2,unit9,unit10")]
    [TestCase("lesson3,lesson10,lesson2", "lesson2,lesson3,lesson10")]
    [TestCase("step2,step1,step10", "step1,step2,step10")]
    [TestCase("m1,m3,m10,m2", "m1,m2,m3,m10")]
    [TestCase("ch1,ch3,ch10,ch2", "ch1,ch2,ch3,ch10")]
    [TestCase("sec1,sec3,sec2", "sec1,sec2,sec3")]
    [TestCase("s2,s1,s10", "s1,s2,s10")]
    [TestCase("p1,p10,p2", "p1,p2,p10")]
    [TestCase("item9,item2,item10", "item2,item9,item10")]
    [TestCase("part1,part3,part10,part2", "part1,part2,part3,part10")]
    [TestCase("f1,f3,f2,f10", "f1,f2,f3,f10")]
    [TestCase("apple100,apple20,apple1", "apple1,apple20,apple100")]
    [TestCase("doc9,doc2,doc10,doc1", "doc1,doc2,doc9,doc10")]
    [TestCase("v2.1,v2.10,v2.2", "v2.1,v2.2,v2.10")]
    [TestCase("step3,step2,step10", "step2,step3,step10")]
    [TestCase("c10,c1,c2,c20", "c1,c2,c10,c20")]
    [TestCase("p10,p1,p2,p3", "p1,p2,p3,p10")]
    [TestCase("q10,q1,q2,q3", "q1,q2,q3,q10")]
    [TestCase("sec1,sec10,sec2,sec20", "sec1,sec2,sec10,sec20")]
    [TestCase("t3,t2,t10,t1", "t1,t2,t3,t10")]
    [TestCase("a,!,b", "!,a,b")]
    [TestCase("a1,b-1,c+1", "a1,b-1,c+1")]
    [TestCase("2a,1a,10a", "1a,2a,10a")]
    [TestCase("item01,item002,item1", "item01,item1,item002")]  // Testing leading zeros
    [TestCase("hello1,hello01,hello10", "hello1,hello01,hello10")]
    [TestCase("item2,item2,item2", "item2,item2,item2")]  // Duplicates
    [TestCase("2dogs,1dog,10dogs", "1dog,2dogs,10dogs")]
    [TestCase("img12.png,img2.jpg,img10.gif,img1.bmp", "img1.bmp,img2.jpg,img10.gif,img12.png")]
    [TestCase("text10.txt,text2.txt,text01.txt", "text01.txt,text2.txt,text10.txt")]
    [TestCase("100a,20a,1a", "1a,20a,100a")]
    [TestCase("b2a3,b2a10,b2a2", "b2a2,b2a3,b2a10")]
    [TestCase("c-1,c-2,c-10", "c-1,c-2,c-10")]
    [TestCase("d1.5,d1.7,d1.10", "d1.5,d1.7,d1.10")]
    [TestCase("1234,123,12345", "123,1234,12345")]  // Pure numbers
    [TestCase("01,002,003,10,2", "01,002,2,003,10")]  // More leading zeros
    [TestCase("a,aa,a1", "a,a1,aa")]  // Text and numbers
    public void ShouldSort(string inputRaw, string expectedRaw)
    {
        //Given
        var comparer = new NaturalStringComparer();

        var input = inputRaw.Split(',');
        var expected = expectedRaw.Split(',');

        //When
        var ordered = input.OrderBy(x => x, comparer).ToArray();

        //Then
        ordered.CollectionSequenceShouldBe(expected);
    }
}