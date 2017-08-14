<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
                 xmlns:kml="http://earth.google.com/kml/2.0"
                version="1.0">
<xsl:output method="text"/>
<xsl:template match="/">
  <xsl:apply-templates></xsl:apply-templates>
</xsl:template>
  
  <xsl:template match="Document">
    <xsl:apply-templates select="Folder"></xsl:apply-templates>
  </xsl:template>

  <xsl:template match="Folder">
    <xsl:apply-templates select="Placemark"></xsl:apply-templates>
    <xsl:apply-templates select="Folder"></xsl:apply-templates>
  </xsl:template>


  <xsl:template match="Placemark">
    <xsl:apply-templates select="name"></xsl:apply-templates>
    <xsl:apply-templates select="Point"></xsl:apply-templates>
    <xsl:apply-templates select="description"></xsl:apply-templates>
  </xsl:template>

  <xsl:template match="name">
    <xsl:text>&#13;&#10;</xsl:text>
    <xsl:apply-templates></xsl:apply-templates>
    <xsl:text>/*888*/</xsl:text>
  </xsl:template>

  <xsl:template match="description">
    <xsl:apply-templates></xsl:apply-templates>
  </xsl:template>

  <xsl:template match="Point">
    <xsl:apply-templates select="coordinates"></xsl:apply-templates>
    <xsl:text>/*888*/</xsl:text>
  </xsl:template>


</xsl:stylesheet>