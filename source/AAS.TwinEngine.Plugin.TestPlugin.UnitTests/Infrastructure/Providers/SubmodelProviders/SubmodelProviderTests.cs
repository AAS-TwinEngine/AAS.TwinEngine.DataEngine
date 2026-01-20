using System.Text.Json;

using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Submodel.Config;
using Aas.TwinEngine.Plugin.TestPlugin.DomainModel.Submodel;
using Aas.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers;
using Aas.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers.SubmodelProviders;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Aas.TwinEngine.Plugin.TestPlugin.UnitTests.Infrastructure.Providers.SubmodelProviders;

public class SubmodelProviderTests
{
    private readonly ILogger<SubmodelProvider> _logger;
    private readonly SubmodelProvider _sut;
    private readonly IOptions<Semantics> _semantics;
    private const string ProductId = "test-submodelId";

    public SubmodelProviderTests()
    {
        _logger = Substitute.For<ILogger<SubmodelProvider>>();
        _semantics = Substitute.For<IOptions<Semantics>>();
        _semantics.Value.Returns(new Semantics { IndexContextPrefix = "_aastwinengine_" });
        SetSubmodelData(TestData.TestSubmodelData);
        _sut = new SubmodelProvider(_logger, _semantics);
    }

    private static void SetSubmodelData(string jsonContent) => MockData.SubmodelData = JsonDocument.Parse(jsonContent);

    [Fact]
    public void EnrichWithData_LeafNode_SetsValueFromJson()
    {
        var leaf = new SemanticLeafNode("Email", DataType.String, null);
        var root = new SemanticBranchNode("root", DataType.Object);
        root.AddChild(leaf);

        _sut.EnrichWithData(root, ProductId);

        Assert.Equal("test@example.com", leaf.Value);
    }

    [Fact]
    public void EnrichWithData_LeafNode_WhenNoDetailsAvailable_SetsEmptyValue()
    {
        var name = new SemanticLeafNode("name", DataType.String, null!);

        _sut.EnrichWithData(name, ProductId);

        Assert.Equal(string.Empty, name.Value);
    }

    [Fact]
    public void EnrichWithData_BranchNodeWithoutLeaves_ReturnsUnmodifiedBranch()
    {
        var branch = new SemanticBranchNode("contactInformation", DataType.Object);

        var result = _sut.EnrichWithData(branch, ProductId);

        Assert.Empty(branch.Children);
        Assert.Same(branch, result);
    }

    [Fact]
    public void EnricWithData_ForLeafWithoutComplexData_SetsValueFromData()
    {
        var root = new SemanticBranchNode("ManufacturerName", DataType.Object);
        var leaf = new SemanticLeafNode("ManufacturerName_en", DataType.String, null!);
        root.AddChild(leaf);

        _sut.EnrichWithData(root, ProductId);

        Assert.Equal("M&M", leaf.Value);
    }

    [Fact]
    public void EnrichWithData_ForBranchNodeWithoutComplexData_SetsValueFormData()
    {
        var contact = new SemanticBranchNode("ContactInformation", DataType.Object);
        contact.AddChild(new SemanticLeafNode("Email", DataType.String, null!));
        contact.AddChild(new SemanticLeafNode("Phone", DataType.String, null!));

        _sut.EnrichWithData(contact, ProductId);

        var email = (SemanticLeafNode)contact.Children
                          .First(c => c.SemanticId == "Email");
        var phone = (SemanticLeafNode)contact.Children
                          .First(c => c.SemanticId == "Phone");
        Assert.Equal("contact@test.com", email.Value);
        Assert.Equal("555-1234", phone.Value);
    }

    [Fact]
    public void EnrichWithData_ForBranchNodeWithComplexData_ClonesChildrenForEachElement_SetsValueFormData()
    {
        var contactInformationBranch = new SemanticBranchNode("ContactInformations", DataType.Object);
        var contactBranch = new SemanticBranchNode("ContactInformation", DataType.Array);
        contactBranch.AddChild(new SemanticLeafNode("Email", DataType.String, null!));
        var phoneBranch = new SemanticBranchNode("Phone", DataType.Object);
        contactBranch.AddChild(phoneBranch);
        phoneBranch.AddChild(new SemanticLeafNode("TelephoneNumber", DataType.String, ""));
        contactInformationBranch.AddChild(contactBranch);

        _sut.EnrichWithData(contactInformationBranch, ProductId);

        var contacts = contactInformationBranch.Children.OfType<SemanticBranchNode>().ToList();
        Assert.Equal(2, contacts.Count);
        var firstContact = contacts[0];
        var firstEmail = firstContact.Children.First(c => c.SemanticId == "Email") as SemanticLeafNode;
        var firstPhone = firstContact.Children.First(c => c.SemanticId == "Phone") as SemanticBranchNode;
        var phoneTelephoneNumber1 = firstPhone.Children.First(c => c.SemanticId == "TelephoneNumber") as SemanticLeafNode;
        Assert.Equal("first@test.com", firstEmail!.Value);
        Assert.Equal("111-1111", phoneTelephoneNumber1.Value);
        var secondContact = contacts[1];
        var secondEmail = secondContact.Children.First(c => c.SemanticId == "Email") as SemanticLeafNode;
        var secondPhone = secondContact.Children.First(c => c.SemanticId == "Phone") as SemanticBranchNode;
        var phoneTelephoneNumber2 = secondPhone.Children.First(c => c.SemanticId == "TelephoneNumber") as SemanticLeafNode;
        Assert.Equal("second@test.com", secondEmail!.Value);
        Assert.Equal("222-2222", phoneTelephoneNumber2!.Value);
    }

    [Fact]
    public void EnrichWithData_ForBranchNodeWithSpecificIndex_SetsValueFormData()
    {
        var contactInformationBranch = new SemanticBranchNode("ContactInformations", DataType.Object);
        var contactBranch = new SemanticBranchNode("ContactInformation_aastwinengine_01", DataType.Object);
        contactBranch.AddChild(new SemanticLeafNode("Email", DataType.String, null!));
        var phoneBranch = new SemanticBranchNode("Phone", DataType.Object);
        contactBranch.AddChild(phoneBranch);
        phoneBranch.AddChild(new SemanticLeafNode("TelephoneNumber", DataType.String, ""));
        var availableBranch = new SemanticBranchNode("AvailableTime", DataType.Object);
        phoneBranch.AddChild(availableBranch);
        availableBranch.AddChild(new SemanticLeafNode("AvailableTime_de", DataType.String, ""));
        contactInformationBranch.AddChild(contactBranch);

        _sut.EnrichWithData(contactInformationBranch, ProductId);

        var email = contactBranch.Children.First(c => c.SemanticId == "Email") as SemanticLeafNode;
        var phone = contactBranch.Children.First(c => c.SemanticId == "Phone") as SemanticBranchNode;
        var phoneTelephoneNumber = phone?.Children.First(c => c.SemanticId == "TelephoneNumber") as SemanticLeafNode;
        Assert.Equal("second@test.com", email!.Value);
        Assert.Equal("222-2222", phoneTelephoneNumber!.Value);
        var available = phone?.Children.First(c => c.SemanticId == "AvailableTime") as SemanticBranchNode;
        var availableTime = available?.Children.First(c => c.SemanticId == "AvailableTime_de") as SemanticLeafNode;
        Assert.Equal("Montag – Freitag 08:00 bis 16:00", availableTime?.Value);
    }

    [Fact]
    public void EnrichWithData_WhenBranchHasChildArray_ClonesNodesAndSetsLeafValues()
    {
        var nameplateNode = new SemanticBranchNode("Nameplate", DataType.Object);
        var contactInfoNode = new SemanticBranchNode("ContactInformation", DataType.Array);
        var phoneNode = new SemanticBranchNode("Phone", DataType.Object);
        var phoneNumberLeaf = new SemanticLeafNode("TelephoneNumber", DataType.String, "");
        phoneNode.AddChild(phoneNumberLeaf);
        contactInfoNode.AddChild(phoneNode);
        nameplateNode.AddChild(contactInfoNode);

        _sut.EnrichWithData(nameplateNode, ProductId);

        var contactInfo = nameplateNode.Children[0] as SemanticBranchNode;
        Assert.Equal(2, contactInfo?.Children.Count);
        var phone1 = contactInfo?.Children[0] as SemanticBranchNode;
        Assert.Single(phone1?.Children!);
        var firstNumber = phone1.Children[0] as SemanticLeafNode;
        Assert.Equal("+49571 8870", firstNumber.Value);
        var phone2 = contactInfo?.Children[1] as SemanticBranchNode;
        Assert.Single(phone1.Children);
        var secondNumber = phone2?.Children[0] as SemanticLeafNode;
        Assert.Equal("+91 7845129532", secondNumber.Value);
    }

    [Fact]
    public void EnrichWithData_WhenBranchHasDeepChildArray_ExpandsArrayAndSetsLeafValues()
    {
        var mcadNode = new SemanticBranchNode("MCAD", DataType.Object);
        var documentStepNode = new SemanticBranchNode("Document_STEP", DataType.Array);
        var documentIdNode = new SemanticBranchNode("DocumentId", DataType.Object);
        var documentVersionNode = new SemanticBranchNode("DocumentVersion", DataType.Object);
        var statusValueLeaf = new SemanticLeafNode("StatusValue", DataType.String, "");
        documentVersionNode.AddChild(statusValueLeaf);
        documentIdNode.AddChild(documentVersionNode);
        documentStepNode.AddChild(documentIdNode);
        mcadNode.AddChild(documentStepNode);

        _sut.EnrichWithData(mcadNode, ProductId);

        var documentStep = mcadNode.Children[0] as SemanticBranchNode;
        var documentId = documentStep.Children[0] as SemanticBranchNode;
        Assert.Equal(2, documentId.Children.Count);
        var firstVersion = documentId.Children[0] as SemanticBranchNode;
        var firstStatus = firstVersion.Children[0] as SemanticLeafNode;
        Assert.Equal("StatusValue", firstStatus.SemanticId);
        Assert.Equal("Released", firstStatus.Value);
        var secondVersion = documentId.Children[1] as SemanticBranchNode;
        var secondStatus = secondVersion.Children[0] as SemanticLeafNode;
        Assert.Equal("StatusValue", secondStatus.SemanticId);
        Assert.Equal("Inprogress", secondStatus.Value);
    }

    [Fact]
    public void EnrichWithData_ForBranchNodeWithSpectificIndex_WithNestedComplexdata_SetsValueFromData()
    {
        var rootBranch = new SemanticBranchNode("Document_aastwinengine_00", DataType.Object);
        rootBranch.AddChild(new SemanticLeafNode("IsPrimary", DataType.String, ""));
        var branch = new SemanticBranchNode("DocumentClassification", DataType.Array);
        rootBranch.AddChild(branch);
        branch.AddChild(new SemanticLeafNode("ClassId", DataType.String, ""));
        branch.AddChild(new SemanticLeafNode("ClassificationSystem", DataType.String, ""));

        _sut.EnrichWithData(rootBranch, ProductId);

        var documents = rootBranch.Children.OfType<SemanticBranchNode>().ToList();
        Assert.Equal(2, documents.Count);
        var primary = rootBranch.Children.First(c => c.SemanticId == "IsPrimary") as SemanticLeafNode;
        Assert.Equal("true", primary.Value);
        var firstDocumentClassification = documents[0];
        var firstClassId = firstDocumentClassification.Children.First(c => c.SemanticId == "ClassId") as SemanticLeafNode;
        var firstClassificationSystem = firstDocumentClassification.Children.First(c => c.SemanticId == "ClassificationSystem") as SemanticLeafNode;
        Assert.Equal("02-02", firstClassId.Value);
        Assert.Equal("VDI2770:2020", firstClassificationSystem.Value);
        var secondDocumentClassification = documents[1];
        var secondClassId = secondDocumentClassification.Children.First(c => c.SemanticId == "ClassId") as SemanticLeafNode;
        var secondClassificationSystem = secondDocumentClassification.Children.First(c => c.SemanticId == "ClassificationSystem") as SemanticLeafNode;
        Assert.Equal("STEP", secondClassId.Value);
        Assert.Equal("IDTA-MCAD:2022", secondClassificationSystem.Value);
    }

    [Fact]
    public void EnrichWithData_ForBranchNodeWithSpectificIndex_WithoutNestedComplexdata_SetsValueFromData()
    {
        var rootBranch = new SemanticBranchNode("Document_aastwinengine_01", DataType.Object);
        rootBranch.AddChild(new SemanticLeafNode("IsPrimary", DataType.String, ""));
        var branch = new SemanticBranchNode("DocumentClassification", DataType.Object);
        rootBranch.AddChild(branch);
        branch.AddChild(new SemanticLeafNode("ClassId", DataType.String, ""));
        branch.AddChild(new SemanticLeafNode("ClassificationSystem", DataType.String, ""));

        _sut.EnrichWithData(rootBranch, ProductId);

        var documents = rootBranch.Children.OfType<SemanticBranchNode>().ToList();
        Assert.Equal(1, documents.Count);
        var primary = rootBranch.Children.First(c => c.SemanticId == "IsPrimary") as SemanticLeafNode;
        Assert.Equal("false", primary.Value);
        var firstDocumentClassification = documents[0];
        var firstClassId = firstDocumentClassification.Children.First(c => c.SemanticId == "ClassId") as SemanticLeafNode;
        var firstClassificationSystem = firstDocumentClassification.Children.First(c => c.SemanticId == "ClassificationSystem") as SemanticLeafNode;
        Assert.Equal("01-01", firstClassId.Value);
        Assert.Equal("VDI2770:2025", firstClassificationSystem.Value);
    }
}
